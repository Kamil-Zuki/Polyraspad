# Введение

Методы группы **Authentication** — вход, обновление JWT-пары и logout с отзывом refresh token.

**SR группы:** SR-AUTHMOD-AUTH-01 … SR-AUTHMOD-AUTH-03. Алгоритмы: [[../Алгоритмы и методы бекенда/01 - JWT и Refresh Token Generation]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AUTHMOD-AUTH-01 | `LoginUser` | Unary | Email/password → access + refresh JWT. |
| SR-AUTHMOD-AUTH-02 | `RefreshToken` | Unary | Обмен refresh token на новую пару. |
| SR-AUTHMOD-AUTH-03 | `LogoutUser` | Unary | Отзыв refresh token (metadata `user_id`). |

---

<span id="grpc-LoginUser"></span>

# SR-AUTHMOD-AUTH-01: Login: LoginUser

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - Аутентификация и JWT-токены (Authentication)#SR-AUTHMOD-AUTH-01]]

**REST-паритет:** `POST /api/v1/auth/login`; Aggregator `POST /api/auth/login`.

| Сигнатура | `rpc LoginUser (LoginUserRequest) returns (TokenResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `email`, `password` |
| **Сообщение ответа** | `access_token`, `refresh_token` |

## Логика обработки запроса

1. Map proto → `UserLoginRequest`; FluentValidation.
2. `AuthService.LoginUserAsync` — найти user по email, `CheckPasswordSignInAsync`.
3. Если email не подтверждён — `INVALID_ARGUMENT` (не выдавать токены).
4. Генерация JWT access + refresh ([[../Алгоритмы и методы бекенда/01 - JWT и Refresh Token Generation]]); сохранить refresh в store.
5. Map → `TokenResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | User not found / invalid password |
| **INVALID_ARGUMENT** | Email not confirmed / validation errors |
| **INTERNAL** | Unhandled exception |

---

<span id="grpc-RefreshToken"></span>

# SR-AUTHMOD-AUTH-02: Refresh: RefreshToken

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - Аутентификация и JWT-токены (Authentication)#SR-AUTHMOD-AUTH-02]]

**REST-паритет:** `POST /api/v1/auth/refresh-token`; Aggregator `POST /api/auth/refresh-token`.

| Сигнатура | `rpc RefreshToken (RefreshTokenRequest) returns (TokenResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `refresh_token` |
| **Сообщение ответа** | New `access_token`, `refresh_token` |

## Логика обработки запроса

1. `AuthService.RefreshToken` — validate refresh token signature и expiry.
2. Проверить token не revoked; rotate refresh (новый refresh, старый invalidate).
3. Emit new access token с актуальными claims.
4. Return `TokenResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | Invalid / expired / revoked refresh |
| **INTERNAL** | Unhandled |

---

<span id="grpc-LogoutUser"></span>

# SR-AUTHMOD-AUTH-03: Logout: LogoutUser

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - Аутентификация и JWT-токены (Authentication)#SR-AUTHMOD-AUTH-03]]

**REST-паритет:** `POST /api/v1/auth/logout` (body: `refreshToken`); Aggregator `POST /api/auth/logout`.

| Сигнатура | `rpc LogoutUser (LogoutUserRequest) returns (MessageResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id` (optional, из metadata), `refresh_token` |
| **Сообщение ответа** | `message` |

## Логика обработки запроса

1. `userId = GrpcContextHelper.GetUserId(context)`; при отсутствии — `UNAUTHENTICATED`.
2. Если `request.user_id` задан и ≠ metadata — `PERMISSION_DENIED`.
3. `AuthService.LogoutUserAsync(userId, refreshToken)` — revoke refresh token в store.
4. Map → `MessageResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | Missing `user_id` metadata |
| **PERMISSION_DENIED** | `user_id` mismatch |
| **NOT_FOUND** | User not found |
| **INVALID_ARGUMENT** | Invalid refresh token payload |
| **INTERNAL** | Unhandled |

---

*Следующая группа: [[03 - Управление профилем (Profile Management)]].*
