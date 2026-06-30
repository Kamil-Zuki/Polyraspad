# Группа 2: Аутентификация и JWT-токены (Authentication)

## Введение

В этом разделе описывается **выдача и обновление JWT**, управление **refresh tokens** и **logout** для Polyraspad.

Access JWT — stateless HMAC-SHA256 (`sub`, `name`, `jti`). Refresh — opaque token в PostgreSQL с rotation и TTL 7 дней. Aggregator валидирует access JWT **локально**; auth-module отвечает за credentials и refresh lifecycle.

**Метафора:**

Представьте **абонement + продление в фитнес-клубе**. Вход по паролю выдаёт **дневной браслет (access JWT)** и **карту продления (refresh)**. Браслет проверяют на турникете (Aggregator) без звонка в офис; продление карты — только в офисе (auth-module), при этом старая карта аннулируется.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к аутентификации и JWT-токенам.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AUTHMOD-AUTH-01** | **Login и выдача JWT:** Password sign-in для confirmed email; пара access + refresh; multi-device refresh allowed. |
| **SR-AUTHMOD-AUTH-02** | **Refresh token rotation:** Valid refresh → new pair; old token revoked. |
| **SR-AUTHMOD-AUTH-03** | **Logout:** SignOut + revoke переданного refresh token для userId. |
| **SR-AUTHMOD-AUTH-04** | **Генерация access JWT:** Issuer/Audience/Secret из config; expiry `Jwt:Expire` minutes. |

---

# Детальная спецификация требований

## SR-AUTHMOD-AUTH-01: Login и выдача JWT {#SR-AUTHMOD-AUTH-01}

Password sign-in для пользователя с подтверждённым email; выдача пары access JWT + refresh token.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Email confirmed** | `EmailConfirmed = false` → login rejected. |
| **No lockout** | `lockoutOnFailure: false` в текущей реализации. |
| **New refresh row** | Каждый login добавляет refresh; старые не отзываются. |
| **Claims** | `sub` = userId, `name` = userName. |

### 2. Высокоуровневое описание

Представим login как **вход в фитнес-клуб с выдачей дневного браслета и карты продления**.

1. **Запрос (Рецепция):** Frontend через Aggregator вызывает gRPC `LoginUser` с email и password; precondition — `EmailConfirmed = true` (SR-AUTHMOD-REG-02).
2. **Поиск пользователя (Картотека):** `UserManager.FindByEmailAsync` находит `ApplicationUser`; отсутствие user или неверный пароль → `PasswordSignInAsync` failed → gRPC `Unauthenticated`.
3. **Email gate (Проверка абонемента):** `EmailConfirmed = false` → login rejected с `InvalidArgument` («Email not confirmed»), даже если пароль верный.
4. **Access JWT (Дневной браслет):** `TokenService.GenerateJwtToken` выпускает HMAC-SHA256 token с claims `sub` = userId, `name` = userName, `jti` = new GUID; TTL из `Jwt:Expire` minutes; `lockoutOnFailure: false`.
5. **Refresh token (Карта продления):** opaque string (64 random bytes, Base64) сохраняется в PostgreSQL как `RefreshToken` entity с `ExpiryDate` +7d; **старые refresh rows не отзываются** — multi-device allowed.
6. **Ответ (TokenResponse):** gRPC возвращает пару `access_token` + `refresh_token`; Aggregator передаёт access JWT клиенту для локальной валидации.

Таким образом, login создаёт **новый refresh row** без отзыва предыдущих (multi-device); access JWT stateless и проверяется Aggregator без gRPC round-trip.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **gRPC:** `LoginUser`.
* **Precondition:** `EmailConfirmed = true`.

#### Сценарий А: Успешный login (Happy Path)

1. **gRPC:** `LoginUser(email, password)`.
2. **Ответ:** `access_token`, `refresh_token`.

#### Сценарий Б: Email не подтверждён (Negative Path)

1. **Domain:** Password OK but `EmailConfirmed = false`.
2. **Ответ:** gRPC `InvalidArgument` — «Email not confirmed».

#### Сценарий В: Неверный пароль (Negative Path)

1. **SignIn:** failed.
2. **Ответ:** gRPC `Unauthenticated` — «Invalid login attempt».

---

## SR-AUTHMOD-AUTH-02: Refresh token rotation {#SR-AUTHMOD-AUTH-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Single use rotation** | Old refresh → `IsRevoked = true`. |
| **Expiry check** | `ExpiryDate < UtcNow` → reject. |
| **Public RPC** | Refresh не требует access JWT (refresh IS credential). |

### 2. Высокоуровневое описание

Представим refresh как **обмен старой карты продления на новую пару tokens**.

1. **Запрос (Касса продления):** клиент вызывает gRPC `RefreshToken` с opaque refresh string; endpoint публичный — access JWT не требуется, refresh **сам** является credential.
2. **Lookup (Сверка в базе):** поиск `RefreshToken` row по token string в PostgreSQL; отсутствие row → reject.
3. **Validate (Проверка срока):** `IsRevoked = true` или `ExpiryDate < UtcNow` → gRPC `Unauthenticated` («Invalid or expired refresh token»).
4. **Rotation (Аннулирование старой карты):** текущий refresh помечается `IsRevoked = true` **до** выдачи новой пары — single-use semantics.
5. **Новая пара (Переоформление):** загрузка user из row → `GenerateJwtToken` (новый access) + новый refresh (64 bytes, Base64) с `ExpiryDate` +7d → сохранение в PostgreSQL.
6. **Ответ (TokenResponse):** клиент получает свежую пару tokens; Aggregator продолжает валидировать access JWT локально по shared `Jwt:Secret`.

Таким образом, refresh token **одноразовый** в рамках rotation — повторное использование отозванного token отклоняется.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Успешный refresh (Happy Path)

1. **gRPC:** `RefreshToken(refresh_token)`.
2. **Ответ:** новая пара tokens.

#### Сценарий Б: Повторное использование старого refresh (Negative Path)

1. **Lookup:** token `IsRevoked = true`.
2. **Ответ:** gRPC `Unauthenticated` — «Invalid or expired refresh token».

---

## SR-AUTHMOD-AUTH-03: Logout {#SR-AUTHMOD-AUTH-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **user_id from metadata** | Aggregator передаёт userId после JWT validation. |
| **Optional refresh revoke** | Если refresh_token в request — revoke matching row. |
| **Access JWT** | Stateless — остаётся valid до exp (client discards). |

### 2. Высокоуровневое описание

Представим logout как **сдачу карты продления при выходе из клуба**.

1. **Идентификация (Проверка на выходе):** Aggregator валидирует access JWT локально (HMAC-SHA256, shared secret) и передаёт `user_id` в gRPC metadata; mismatch с полем request → `PermissionDenied`.
2. **SignOut (Identity session):** `SignInManager.SignOutAsync` завершает cookie/session context ASP.NET Core Identity на стороне auth-module.
3. **Revoke refresh (Аннулирование карты):** если `refresh_token` в теле `LogoutUser` — matching `RefreshToken` row для userId помечается `IsRevoked = true`; цепочка refresh прерывается.
4. **Access JWT (Браслет до exp):** stateless token остаётся cryptographically valid до `exp`; клиент обязан discard — сервер не ведёт blacklist access tokens.
5. **Ответ:** `"Logout successful"`; без refresh в request sign-out выполняется, но конкретная refresh row не отзывается.

Таким образом, access JWT остаётся valid до `exp`, но refresh chain прерывается при logout с переданным refresh token.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Logout с refresh (Happy Path)

1. **gRPC:** `LogoutUser` + metadata user_id + refresh_token body.
2. **Domain:** refresh revoked.
3. **Ответ:** `"Logout successful"`.

---

## SR-AUTHMOD-AUTH-04: Генерация access JWT {#SR-AUTHMOD-AUTH-04}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Algorithm** | HMAC-SHA256 symmetric key from `Jwt:Secret`. |
| **Issuer/Audience** | `Jwt:Issuer`, `Jwt:Audience` — shared with Aggregator. |
| **TTL** | `Jwt:Expire` minutes (default 30 in appsettings). |
| **jti** | New GUID per token issuance. |

### 2. Высокоуровневое описание

Представим access JWT как **штамп с подписью клуба, который турникет проверяет сам**.

1. **Symmetric key (Общий секрет):** `TokenService` читает `Jwt:Secret` (≥ 32 chars в production validation); алгоритм HMAC-SHA256 — symmetric signing.
2. **Issuer/Audience (Штамп клуба):** claims `iss` = `Jwt:Issuer`, `aud` = `Jwt:Audience` — **shared with Aggregator** для локальной валидации без gRPC.
3. **Identity claims (Данные на браслете):** `sub` = userId (GUID string), `name` = userName из Identity store; `jti` = new GUID на каждую выдачу.
4. **TTL (Срок действия):** `exp` вычисляется из `Jwt:Expire` minutes (default 30 в appsettings); после exp Aggregator отклоняет token → клиент идёт на refresh.
5. **Точки вызова:** `GenerateJwtToken(userId, userName)` используется в login (SR-AUTHMOD-AUTH-01) и refresh (SR-AUTHMOD-AUTH-02) flows; Aggregator валидирует token локально на каждый REST call.

Таким образом, **общий секрет JWT** shared с Aggregator — BFF валидирует access без gRPC round-trip; auth-module остаётся единственным issuer.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Token claims (Happy Path)

1. **Login/Refresh** вызывает `GenerateJwtToken`.
2. **JWT** содержит `sub`, `name`, `jti`, `iss`, `aud`, `exp`.

---

*Следующая группа: [[03 - Управление профилем (Profile Management)]].*
