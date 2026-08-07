# Введение

Методы группы **Registration** — создание учётной записи ASP.NET Core Identity, повторная отправка писем и подтверждение email через SMTP.

**SR группы:** SR-AUTHMOD-REG-01, SR-AUTHMOD-REG-02, SR-AUTHMOD-REG-03. Proto: [[authorization.proto]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AUTHMOD-REG-01 | `RegisterUser` | Unary | Регистрация email/password, отправка confirm link. |
| SR-AUTHMOD-REG-02 | `ConfirmEmail` | Unary | Подтверждение email по user_id + token. |
| SR-AUTHMOD-REG-03 | `ResendConfirmationEmail` | Unary | Повторная отправка письма подтверждения. |

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

---

<span id="grpc-ResendConfirmationEmail"></span>

# SR-AUTHMOD-REG-03: Resend confirmation: ResendConfirmationEmail

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Регистрация и подтверждение email (Registration)#SR-AUTHMOD-REG-03]]

**REST-паритет:** `POST /api/v1/auth/resend-confirmation`; Aggregator `POST /api/auth/resend-confirmation`.

| Сигнатура | `rpc ResendConfirmationEmail (ResendConfirmationEmailRequest) returns (MessageResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `email` |
| **Сообщение ответа** | `message` |

## Логика обработки запроса

1. Map → `ResendConfirmationEmailRequest` DTO.
2. `AuthService.ResendConfirmationEmailAsync` — у неактивированного пользователя генерируется новый token и отправляется письмо через SMTP.
3. Вернуть `MessageResponse`.

---

*Следующая группа: [[02 - Аутентификация и JWT-токены (Authentication)]].*
