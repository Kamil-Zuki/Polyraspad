# Введение

Методы группы **Registration** — создание учётной записи ASP.NET Core Identity и подтверждение email через SMTP.

**SR группы:** SR-AUTHMOD-REG-01, SR-AUTHMOD-REG-02. Proto: [[authorization.proto]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AUTHMOD-REG-01 | `RegisterUser` | Unary | Регистрация email/password, отправка confirm link. |
| SR-AUTHMOD-REG-02 | `ConfirmEmail` | Unary | Подтверждение email по user_id + token. |

---

<span id="grpc-RegisterUser"></span>

# SR-AUTHMOD-REG-01: Регистрация: RegisterUser

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Регистрация и подтверждение email (Registration)#SR-AUTHMOD-REG-01]]

**REST-паритет (legacy):** `POST /api/v1/auth/register`. **Публичный BFF:** `POST /api/auth/register` (Aggregator).

| Сигнатура | `rpc RegisterUser (RegisterUserRequest) returns (RegisterUserResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `email`, `password`, `confirm_password` |
| **Сообщение ответа** | `message` — текст для UI |

## Логика обработки запроса

1. Map proto → `UserRegistrationRequest`; FluentValidation.
2. `AuthService.RegisterUserAsync` — создать пользователя в Identity, хеш пароля, `EmailConfirmed = false`.
3. Отправить SMTP confirm link ([[../Интеграции со сторонними сервисами/01 - SMTP Email (Confirm)]]).
4. Map результат → `RegisterUserResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Validation fail, duplicate confirmed email, SMTP misconfiguration |
| **INTERNAL** | Необработанное исключение |

---

<span id="grpc-ConfirmEmail"></span>

# SR-AUTHMOD-REG-02: Confirm email: ConfirmEmail

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Регистрация и подтверждение email (Registration)#SR-AUTHMOD-REG-02]]

**REST-паритет:** `GET /api/v1/auth/confirm-email?userId=&token=`; Aggregator `GET /api/auth/confirm-email`.

| Сигнатура | `rpc ConfirmEmail (ConfirmEmailRequest) returns (MessageResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id` (UUID string), `token` |
| **Сообщение ответа** | `message` |

## Логика обработки запроса

1. Map → `ConfirmEmailRequest` DTO; parse `user_id` как Guid.
2. `AuthService.ConfirmEmailAsync` — Identity `ConfirmEmailAsync` с token provider.
3. При успехе — `EmailConfirmed = true`; вернуть `MessageResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Invalid token, user not found, expired token |
| **INTERNAL** | Необработанное исключение |

---

*Следующая группа: [[02 - Аутентификация и JWT-токены (Authentication)]].*
