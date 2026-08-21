# Введение

Методы группы **Profile Management** — чтение профиля, lookup по email и мутации username/password/avatar.

Protected RPC ожидают inbound metadata `user_id` от Aggregator после JWT validation.

**SR группы:** SR-AUTHMOD-PROF-01 … SR-AUTHMOD-PROF-05.

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AUTHMOD-PROF-01 | `GetUserInfo` | Unary | Профиль текущего пользователя. |
| SR-AUTHMOD-PROF-05 | `FindUserByEmail` | Unary | Lookup для sharing (internal callers). |
| SR-AUTHMOD-PROF-02 | `UpdateUsername` | Unary | Смена display name. |
| SR-AUTHMOD-PROF-03 | `UpdatePassword` | Unary | Смена пароля с проверкой current. |
| SR-AUTHMOD-PROF-04 | `UpdateAvatarUrl` | Unary | URL аватара; пустая строка — сброс. |

---

<span id="grpc-GetUserInfo"></span>

# SR-AUTHMOD-PROF-01: Profile read: GetUserInfo

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Управление профилем (Profile Management)#SR-AUTHMOD-PROF-01]]

**REST-паритет:** `GET /api/v1/auth/me`; Aggregator `GET /api/auth/me`.

| Сигнатура | `rpc GetUserInfo (GetUserInfoRequest) returns (UserInfoResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id` (optional; canonical source — metadata) |
| **Сообщение ответа** | `id`, `user_name`, `email`, `email_confirmed`, `avatar_url` |

## Логика обработки запроса

1. `userId = GrpcContextHelper.GetUserId(context)`.
2. Optional: если `request.user_id` задан и ≠ metadata → `PERMISSION_DENIED`.
3. `AuthService.GetUserInfoAsync(userId)` → map `UserInfoResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | Missing `user_id` metadata |
| **PERMISSION_DENIED** | `user_id` mismatch |
| **NOT_FOUND** | User not found |
| **INTERNAL** | Unhandled |

---

<span id="grpc-FindUserByEmail"></span>

# SR-AUTHMOD-PROF-05: Lookup: FindUserByEmail

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Управление профилем (Profile Management)#SR-AUTHMOD-PROF-05]]

Internal RPC для Reader Library sharing и collaborator lookup. **Не** публичный REST на Aggregator.

| Сигнатура | `rpc FindUserByEmail (FindUserByEmailRequest) returns (UserInfoResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `email` |
| **Сообщение ответа** | `UserInfoResponse` (id, user_name, email, …) |

## Логика обработки запроса

1. Validate non-empty email.
2. `AuthService.FindUserByEmailAsync(email)` — Identity lookup.
3. Map user → `UserInfoResponse` или `NOT_FOUND`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Empty email |
| **NOT_FOUND** | User not found |
| **INTERNAL** | Unhandled |

---

<span id="grpc-UpdateUsername"></span>

# SR-AUTHMOD-PROF-02: Rename: UpdateUsername

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Управление профилем (Profile Management)#SR-AUTHMOD-PROF-02]]

**REST-паритет:** `PUT /api/v1/auth/username`; Aggregator `PUT /api/auth/username`.

| Сигнатура | `rpc UpdateUsername (UpdateUsernameRequest) returns (MessageResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_name` |
| **Сообщение ответа** | `message` |

## Логика обработки запроса

1. `userId` из metadata; mismatch check на `request.user_id`.
2. `AuthService.UpdateUserNameAsync(userId, request.UserName)`.
3. Map → `MessageResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | Missing metadata |
| **PERMISSION_DENIED** | `user_id` mismatch |
| **NOT_FOUND** | User not found |
| **INVALID_ARGUMENT** | Empty or invalid username |
| **INTERNAL** | Unhandled |

---

<span id="grpc-UpdatePassword"></span>

# SR-AUTHMOD-PROF-03: Password change: UpdatePassword

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Управление профилем (Profile Management)#SR-AUTHMOD-PROF-03]]

**REST-паритет:** `PUT /api/v1/auth/password`; Aggregator `PUT /api/auth/password`.

| Сигнатура | `rpc UpdatePassword (UpdatePasswordRequest) returns (MessageResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `current_password`, `new_password` |
| **Сообщение ответа** | `message` |

## Логика обработки запроса

1. `userId` из metadata; mismatch check.
2. `AuthService.UpdateUserPasswordAsync(userId, current, new)` — verify current, rehash new.
3. Map → `MessageResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | Missing metadata |
| **PERMISSION_DENIED** | `user_id` mismatch |
| **NOT_FOUND** | User not found |
| **INVALID_ARGUMENT** | Wrong current password / validation |
| **INTERNAL** | Unhandled |

---

<span id="grpc-UpdateAvatarUrl"></span>

# SR-AUTHMOD-PROF-04: Avatar URL: UpdateAvatarUrl

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Управление профилем (Profile Management)#SR-AUTHMOD-PROF-04]]

**REST-паритет:**

| Поверхность | Route | Статус |
| :--- | :--- | :--- |
| Aggregator BFF | `PUT /api/auth/avatar-url` | ✅ реализован |
| Legacy `AccountsController` | `/api/v1/auth/avatar*` | ❌ **не реализован** — avatar только через Aggregator + gRPC |

| Сигнатура | `rpc UpdateAvatarUrl (UpdateAvatarUrlRequest) returns (MessageResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `avatar_url` — пустая строка очищает поле |
| **Сообщение ответа** | `message` |

## Логика обработки запроса

1. `userId` из metadata; mismatch check.
2. `AuthService.UpdateAvatarUrlAsync(userId, request.AvatarUrl)` — update Identity user field.
3. Map → `MessageResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | Missing metadata |
| **PERMISSION_DENIED** | `user_id` mismatch |
| **NOT_FOUND** | User not found |
| **INVALID_ARGUMENT** | Invalid URL format (if validated) |
| **INTERNAL** | Unhandled |

---

*Конец gRPC profile group. Operations (health, CORS) — [[../Алгоритмы и методы бекенда/04 - Platform Operations]].*
