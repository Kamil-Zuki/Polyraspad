# Группа 1: Регистрация и подтверждение email (Registration)

## Введение

В этом разделе описывается **onboarding** пользователя Polyraspad: создание учётной записи через email/password и **обязательное подтверждение email** перед первым login.

Сервис использует ASP.NET Core Identity (`UserManager.CreateAsync`, `GenerateEmailConfirmationTokenAsync`) и SMTP (`IEmailService`) для отправки ссылки. JWT **не выдаётся** на этапе регистрации.

**Метафора:**

Представьте **регистрацию в библиотеке с пропуском по почте**. Вы заполняете анкету (email, пароль), библиотека заводит карточку читателя, но **читательский билет (JWT) выдадут только после того**, как вы перейдёте по ссылке из письма и подтвердите адрес.

gRPC: `RegisterUser`, `ConfirmEmail`. REST legacy: `POST /api/v1/auth/register`, `GET /api/v1/auth/confirm-email`.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к регистрации и подтверждению email.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AUTHMOD-REG-01** | **Регистрация пользователя:** Валидация email/password, создание Identity user с auto username, SMTP confirm link; duplicate confirmed email → ошибка. |
| **SR-AUTHMOD-REG-02** | **Подтверждение email:** Callback с userId + token; Identity ConfirmEmailAsync; удаление неподтверждённых дубликатов по email. |

---

# Детальная спецификация требований

## SR-AUTHMOD-REG-01: Регистрация пользователя {#SR-AUTHMOD-REG-01}

Новый пользователь Polyraspad регистрируется по email и паролю. До подтверждения email login заблокирован.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **FluentValidation** | Email format, password rules, confirmPassword match до вызова domain. |
| **Auto username** | `User_{8 hex}` — пользователь может сменить позже (SR-AUTHMOD-PROF-02). |
| **Duplicate guard** | Confirmed user с тем же email → `ResponseException`. |
| **No JWT on register** | Ответ — текст «Confirm your email». |
| **SMTP required** | Ошибка отправки письма → register fails. |

### 2. Высокоуровневое описание

Представим регистрацию как **анкету нового читателя с письмом-подтверждением на почту**.

1. **Запрос (Анкета):** Frontend через Aggregator отправляет gRPC `RegisterUser` с email, password и confirmPassword; FluentValidation проверяет формат и совпадение паролей до вызова domain.
2. **Проверка дубликата (Картотека):** `UserManager.FindByEmailAsync` — если пользователь с тем же email уже существует и `EmailConfirmed = true`, регистрация отклоняется (`ResponseException`).
3. **Создание учётной записи (Карточка читателя):** `UserManager.CreateAsync` создаёт `ApplicationUser` с auto username `User_{8 hex}`, `EmailConfirmed = false`; пароль хешируется через ASP.NET Core Identity.
4. **Генерация токена (Код подтверждения):** `GenerateEmailConfirmationTokenAsync` формирует одноразовый Identity token; URL собирается как `{ConfirmationLink}={userId}&token={encoded}`.
5. **Отправка письма (SMTP):** `IEmailService` доставляет письмо «Confirm your email»; ошибка SMTP прерывает регистрацию — user row не остаётся без уведомления.

Таким образом, регистрация **не выдаёт JWT** — только инициирует confirm flow через Identity token и SMTP; login заблокирован до SR-AUTHMOD-REG-02.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Инициатор:** Frontend через Aggregator.
* **gRPC:** `RegisterUser`.

#### Сценарий А: Успешная регистрация (Happy Path)

1. **gRPC:** `RegisterUser` с валидными полями.
2. **Domain:** user created, email queued.
3. **Ответ:** `RegisterUserResponse.message = "Confirm your email"`.

#### Сценарий Б: Email уже подтверждён (Negative Path)

1. **Domain:** `FindByEmailAsync` → user with `EmailConfirmed = true`.
2. **Ответ:** gRPC `InvalidArgument` — «Confirmed user with such email already exists».

#### Сценарий В: Слабый пароль (Negative Path)

1. **Validation:** password без uppercase/special → `InvalidArgument` с текстом FluentValidation.

---

## SR-AUTHMOD-REG-02: Подтверждение email {#SR-AUTHMOD-REG-02}

Пользователь переходит по ссылке из письма; сервис активирует учётную запись.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Identity token** | `ConfirmEmailAsync(user, token)` — единственный источник валидности. |
| **Cleanup** | После confirm удаляются другие **неподтверждённые** users с тем же email. |
| **Public endpoint** | Не требует JWT; token одноразовый (Identity semantics). |

### 2. Высокоуровневое описание

Представим подтверждение email как **активацию пропуска по ссылке из письма**.

1. **Callback (Переход по ссылке):** Frontend или Aggregator вызывает gRPC `ConfirmEmail`, передавая `userId` и `token` из query string письма; endpoint публичный — JWT не требуется.
2. **Поиск пользователя (Картотека):** `UserManager.FindByIdAsync` загружает `ApplicationUser`; отсутствие user или невалидный token → `InvalidArgument` с описаниями Identity.
3. **Подтверждение email (Активация):** `ConfirmEmailAsync(user, token)` — единственный источник валидности; при успехе `EmailConfirmed = true`, token одноразовый по семантике Identity.
4. **Очистка дубликатов (Уборка картотеки):** после confirm удаляются другие **неподтверждённые** users с тем же email — stale rows не блокируют повторную регистрацию.
5. **Ответ клиенту (Готовность к login):** возвращается `"Confirmation completed successfully"`; JWT по-прежнему не выдаётся — пользователь переходит к login (SR-AUTHMOD-AUTH-01).

Таким образом, confirm link — **единственный gate** перед первым login; без успешного `ConfirmEmailAsync` password sign-in отклоняется.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **gRPC:** `ConfirmEmail`.
* **Auth:** public endpoint, JWT не требуется.

#### Сценарий А: Успешное подтверждение (Happy Path)

1. **gRPC:** `ConfirmEmail(user_id, token)`.
2. **Domain:** `EmailConfirmed = true`; stale unconfirmed rows removed.
3. **Ответ:** `"Confirmation completed successfully"`.

#### Сценарий Б: Невалидный token (Negative Path)

1. **Identity:** ConfirmEmailAsync fails.
2. **Ответ:** gRPC `InvalidArgument` с Identity error descriptions.

---

*Следующая группа: [[02 - Аутентификация и JWT-токены (Authentication)]].*
