# Группа 3: Управление профилем (Profile Management)

## Введение

В этом разделе описываются операции **self-service профиля** и **внутренний lookup пользователя по email** для сценариев совместного доступа (sharing).

Protected операции получают `userId` из gRPC metadata (`user_id` header), который Aggregator извлекает из JWT claim `sub`. Mismatch между metadata и полем `user_id` в request → `PermissionDenied`.

**Метафора:**

Представьте **личный кабинет на reception desk**. Сотрудник (Aggregator) уже проверил ваш паспорт и передаёт ваш ID внутренней системе; вы меняете display name, пароль или фото — но только **свой** профиль. Отдельная справочная функция «найти коллегу по email» доступна внутренним сценариям без impersonation.

REST/gRPC: `GetUserInfo`, `UpdateUsername`, `UpdatePassword`, `UpdateAvatarUrl`, `FindUserByEmail`.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к управлению профилем.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AUTHMOD-PROF-01** | **Профиль текущего пользователя:** UserInfoResponse по userId из gRPC metadata; password hash не возвращается. |
| **SR-AUTHMOD-PROF-02** | **Смена username:** Unique check через UserManager; пустое имя → InvalidArgument. |
| **SR-AUTHMOD-PROF-03** | **Смена пароля:** ChangePasswordAsync с проверкой current password через Identity. |
| **SR-AUTHMOD-PROF-04** | **Смена avatar URL:** Абсолютный http/https URI или сброс; max 2048 символов. |
| **SR-AUTHMOD-PROF-05** | **FindUserByEmail:** Внутренний lookup для sharing; NotFound если email не найден. |

---

# Детальная спецификация требований

## SR-AUTHMOD-PROF-01: Профиль текущего пользователя {#SR-AUTHMOD-PROF-01}

Клиент получает актуальные атрибуты профиля авторизованного пользователя. Identity берётся из metadata, не из тела запроса.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Metadata trust** | userId = `GrpcContextHelper.GetUserId(context)`. |
| **Fields** | id, userName, email, emailConfirmed, avatarUrl. |
| **No password leak** | Password hash never returned. |

### 2. Высокоуровневое описание

Представим GetUserInfo как **просмотр личного дела на reception desk**.

1. **JWT validation (Проверка паспорта):** Aggregator валидирует access JWT локально (HMAC-SHA256, claim `sub`), извлекает userId и передаёт его в gRPC metadata header `user_id`.
2. **Metadata trust (Доверие к BFF):** auth-module читает identity через `GrpcContextHelper.GetUserId(context)` — **не** из тела proto; mismatch metadata vs поле `user_id` в request → `PermissionDenied`.
3. **Load user (Картотека Identity):** `UserManager.FindByIdAsync` загружает `ApplicationUser` из PostgreSQL; user not found → error; missing metadata → gRPC `Unauthenticated`.
4. **Mapping (Карточка профиля):** маппинг в `UserInfoResponse`: id, userName, email, emailConfirmed, avatarUrl.
5. **No password leak (Безопасность):** password hash и security stamps **никогда** не возвращаются клиенту.

Таким образом, **источник identity — metadata от BFF**, а не поле `user_id` в proto (если передано — сверяется на mismatch).

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Инициатор:** Frontend через Aggregator.
* **gRPC:** `GetUserInfo`.
* **Metadata:** `user_id` обязателен.

#### Сценарий А: Get profile (Happy Path)

1. **gRPC:** `GetUserInfo` + metadata `user_id`.
2. **Ответ:** `UserInfoResponse`.

#### Сценарий Б: Missing metadata (Negative Path)

1. **Helper:** no user_id header/claim.
2. **Ответ:** gRPC `Unauthenticated`.

---

## SR-AUTHMOD-PROF-02: Смена username {#SR-AUTHMOD-PROF-02}

Пользователь меняет отображаемое имя (`UserName`) в Identity store.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Non-empty** | Empty username → InvalidArgument. |
| **Uniqueness** | `FindByNameAsync` conflict → «Username is already taken». |
| **Self only** | userId только из metadata. |

### 2. Высокоуровневое описание

Представим смену username как **переименование бейджа на reception desk**.

1. **Identity context (Self only):** Aggregator передаёт `user_id` в gRPC metadata после JWT validation; операция только для **своего** профиля.
2. **Validation (Проверка имени):** gRPC `UpdateUsername` — empty/whitespace username → gRPC `InvalidArgument`.
3. **Uniqueness (Картотека имён):** `UserManager.FindByNameAsync` — conflict с другим user → «Username is already taken» (`InvalidArgument`).
4. **Update (Identity store):** `UserManager.UpdateAsync` сохраняет новый `UserName` в PostgreSQL; email и password не затрагиваются.
5. **JWT claim lag (Старый браслет):** access JWT сохраняет старый `name` claim до refresh или re-login — автоматического перевыпуска JWT нет.

Таким образом, смена username **не затрагивает** email и не перевыпускает JWT автоматически — access token сохраняет старый `name` claim до refresh.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **gRPC:** `UpdateUsername`.
* **Body:** `user_name`.

#### Сценарий А: Rename (Happy Path)

1. **gRPC:** `UpdateUsername` + new user_name.
2. **Ответ:** `"Username updated successfully"`.

#### Сценарий Б: Username занят (Negative Path)

1. **Domain:** другой user с тем же UserName.
2. **Ответ:** gRPC `InvalidArgument`.

---

## SR-AUTHMOD-PROF-03: Смена пароля {#SR-AUTHMOD-PROF-03}

Self-service смена пароля с подтверждением текущего пароля.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **ChangePasswordAsync** | Requires correct current_password. |
| **Identity errors** | Wrong current password → InvalidArgument. |
| **Validation** | New password — Identity password rules. |

### 2. Высокоуровневое описание

Представим смену пароля как **смену кодового слова в сейфе с проверкой старого**.

1. **Identity context (Self only):** userId из gRPC metadata (`user_id` header); `UserManager.FindByIdAsync` загружает user.
2. **Current password (Подтверждение):** gRPC `UpdatePassword` принимает `current_password` и `new_password`; неверный current → `ChangePasswordAsync` fails → `InvalidArgument`.
3. **New password rules (Identity policy):** новый пароль проходит ASP.NET Core Identity password validators (uppercase, special, length).
4. **Persist (Хеш в БД):** `UserManager.ChangePasswordAsync(user, currentPassword, newPassword)` обновляет password hash в PostgreSQL.
5. **Refresh tokens (Сессии):** массовый revoke refresh rows **не выполняется** — активные refresh tokens остаются valid до logout/rotation.

Таким образом, **refresh tokens не отзываются** массово при смене пароля в текущей реализации — только новый login/refresh flow.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Успешная смена (Happy Path)

1. **gRPC:** `UpdatePassword` с верным current_password.
2. **Ответ:** `"Password updated successfully"`.

#### Сценарий Б: Неверный текущий пароль (Negative Path)

1. **Identity:** ChangePasswordAsync fails.
2. **Ответ:** gRPC `InvalidArgument`.

---

## SR-AUTHMOD-PROF-04: Смена avatar URL {#SR-AUTHMOD-PROF-04}

Обновление или сброс публичного URL аватара (`ApplicationUser.AvatarUrl`).

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Empty clears** | Whitespace/empty → `AvatarUrl = null`. |
| **URI validation** | Must be absolute http/https. |
| **Length** | Max 2048 characters. |
| **gRPC only** | REST route отсутствует (см. ISSUE-001). |

### 2. Высокоуровневое описание

Представим avatar URL как **обновление фото на бейдже ссылкой на внешний архив**.

1. **Identity context (Self only):** userId из gRPC metadata; gRPC-only endpoint — REST route отсутствует (см. ISSUE-001).
2. **Empty clears (Сброс фото):** whitespace/empty `avatar_url` → `ApplicationUser.AvatarUrl = null`.
3. **URI validation (Проверка ссылки):** absolute http/https URI required; relative path, ftp → `InvalidArgument`; max 2048 characters.
4. **Persist (Identity store):** trim/validate → update entity → `UserManager.UpdateAsync` в PostgreSQL.
5. **Ответ:** `"Avatar updated successfully"`; бинарные файлы auth-module **не загружает**.

Таким образом, avatar — **ссылка на внешнее хранилище** (MinIO/CDN); auth-module не загружает бинарные файлы.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Set avatar URL (Happy Path)

1. **gRPC:** `UpdateAvatarUrl` с https URL.
2. **Ответ:** `"Avatar updated successfully"`.

#### Сценарий Б: Invalid URL (Negative Path)

1. **Domain:** relative path or ftp scheme.
2. **Ответ:** gRPC `InvalidArgument`.

---

## SR-AUTHMOD-PROF-05: FindUserByEmail {#SR-AUTHMOD-PROF-05}

Внутренний lookup пользователя по email для collaboration/sharing flows.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Internal use** | Vocabulary sharing / collaboration lookup. |
| **Trim email** | Whitespace normalized. |
| **NotFound** | Unknown email → gRPC NotFound. |
| **Public fields only** | Same UserInfoResponse shape. |

### 2. Высокоуровневое описание

Представим FindUserByEmail как **справочник «найти коллегу по email» на reception desk**.

1. **Internal caller (Trusted context):** gRPC `FindUserByEmail` вызывается из Aggregator / Vocabulary sharing flows; **user metadata не требуется**.
2. **Normalize (Нормализация):** email string trim/whitespace normalized перед lookup.
3. **Lookup (Identity store):** `UserManager.FindByEmailAsync` ищет `ApplicationUser` в PostgreSQL.
4. **NotFound (Не найден):** unknown email → gRPC `NotFound`.
5. **Response (Public fields):** map to `UserInfoResponse` — id, userName, email, emailConfirmed, avatarUrl; password hash never returned.

Таким образом, endpoint **не требует user metadata** — вызывается из trusted internal context (Aggregator → Vocabulary sharing).

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **gRPC:** `FindUserByEmail`.
* **Caller:** Aggregator / internal service.

#### Сценарий А: Lookup for invite (Happy Path)

1. **gRPC:** `FindUserByEmail(email)`.
2. **Ответ:** `UserInfoResponse` (public profile fields only).

#### Сценарий Б: Email не найден (Negative Path)

1. **Domain:** user null.
2. **Ответ:** gRPC `NotFound`.

---

*Следующая группа: [[04 - Платформенные контракты (Operations)]].*
