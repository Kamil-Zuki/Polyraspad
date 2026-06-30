# Введение

Проксирование аутентификации к **authorization-module** (`Pvs.Auth.Grpc.AuthService`). Сверено с `AggregatorService/Controllers/AuthController.cs` и `authorization.proto`.

Публичные методы защищены rate limit policy `auth-public`. Маршрут контроллера: `api/Auth/*` (Kestrel case-insensitive; в таблице — lowercase).

# 1. Список эндпоинтов

| SR | Method | Endpoint | gRPC (`authorization.proto`) |
| :--- | :--- | :--- | :--- |
| SR-AGG-AUTH-01 | POST | `/api/auth/register` | `RegisterUser` |
| SR-AGG-AUTH-02 | POST | `/api/auth/login` | `LoginUser` |
| SR-AGG-AUTH-03 | POST | `/api/auth/refresh-token` | `RefreshToken` |
| SR-AGG-AUTH-04 | GET | `/api/auth/confirm-email` | `ConfirmEmail` |
| SR-AGG-AUTH-05 | GET | `/api/auth/me` | `GetUserInfo` |
| SR-AGG-AUTH-06 | POST | `/api/auth/logout` | `LogoutUser` |
| SR-AGG-AUTH-07 | PUT | `/api/auth/username` | `UpdateUsername` |
| SR-AGG-AUTH-07 | PUT | `/api/auth/password` | `UpdatePassword` |
| SR-AGG-AUTH-07 | PUT | `/api/auth/avatar-url` | `UpdateAvatarUrl` |

---

# SR-AGG-AUTH-02: Вход пользователя: POST /api/auth/login

## Общая информация

Выдача access/refresh JWT после проверки credentials в authorization-module.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | [UserLoginDto](../DTO/01%20-%20Аутентификация%20и%20профиль%20(Auth).md#dto-UserLoginDto) |
| **DTO успешного ответа** | [TokenResponseDto](../DTO/01%20-%20Аутентификация%20и%20профиль%20(Auth).md#dto-TokenResponseDto) |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* BFF принимает JSON body
* BFF вызывает gRPC [`LoginUser`](../../../../Authorization%20Module/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Аутентификация%20и%20профиль%20(Auth).md#grpc-LoginUser) на `AuthService`
* Маппинг `RpcException` → 401/400/502

## Успешный ответ

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "...",
  "expiresIn": 3600
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | InvalidArgument |
| **401 Unauthorized** | Unauthenticated |
| **429 Too Many Requests** | Rate limit auth-public |
| **502 Bad Gateway** | Downstream unavailable |

---

# SR-AGG-AUTH-05: Профиль: GET /api/auth/me

## Общая информация

Текущий пользователь по JWT claims → gRPC GetUserInfo.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | [UserInfoDto](../DTO/01%20-%20Аутентификация%20и%20профиль%20(Auth).md#dto-UserInfoDto) |

## Логика обработки запроса

* JWT validation middleware
* `MappingHelper.GetUserId` из claims
* gRPC **`GetUserInfo`** с user_id

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | Missing/invalid JWT |
| **404** | User not found |
| **502** | Auth service error |

---

# SR-AGG-AUTH-01: Регистрация: POST /api/auth/register

## Общая информация

Создание аккаунта; email confirmation link отправляется authorization-module.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | UserRegistrationDto |
| **DTO успешного ответа** | AuthResponseDto |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* Rate limit policy **`auth-public`**
* gRPC [`RegisterUser`](../../../../Authorization%20Module/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Аутентификация%20и%20профиль%20(Auth).md#grpc-RegisterUser)

## Успешный ответ

HTTP **201**, `AuthResponseDto`.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | InvalidArgument |
| **409** | Email already exists |
| **429** | Rate limit |
| **502** | Downstream |

---

# SR-AGG-AUTH-03: Refresh token: POST /api/auth/refresh-token

## Общая информация

Обновление access token по refresh token.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | RefreshTokenDto |
| **DTO успешного ответа** | TokenResponseDto |

## Логика обработки запроса

* Rate limit **`auth-public`**
* gRPC **`RefreshToken`**

## Успешный ответ

HTTP **200**, token pair (как login).

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | InvalidArgument |
| **401** | Invalid/expired refresh token |
| **429** | Rate limit |
| **502** | Downstream |

---

# SR-AGG-AUTH-04: Confirm email: GET /api/auth/confirm-email

## Общая информация

Подтверждение email по token из письма.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | Query: `userId`, `token` |
| **DTO успешного ответа** | AuthResponseDto or redirect semantics |

## Логика обработки запроса

* Rate limit **`auth-public`**
* gRPC **`ConfirmEmail`**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Invalid token |
| **404** | User not found |
| **502** | Downstream |

---

# SR-AGG-AUTH-06: Logout: POST /api/auth/logout

## Общая информация

Инвалидация refresh token / session.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | RefreshTokenDto (optional body) |
| **DTO успешного ответа** | N/A |

## Логика обработки запроса

* JWT required
* gRPC [`LogoutUser`](../../../../Authorization%20Module/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Аутентификация%20и%20профиль%20(Auth).md#grpc-LogoutUser)
* HTTP **204 No Content**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-AUTH-07: Update profile: PUT /api/auth/username | password | avatar-url

## Общая информация

Обновление username, password или avatar URL текущего пользователя.

| Тип метода | PUT |
| :--- | :--- |
| **DTO запроса** | UpdateUsernameDto / UpdatePasswordDto / UpdateAvatarUrlDto |
| **DTO успешного ответа** | UserInfoDto or success message |

## Логика обработки запроса

* JWT → userId
* gRPC **`UpdateUsername`**, **`UpdatePassword`**, **`UpdateAvatarUrl`**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Validation |
| **401** | JWT |
| **502** | Downstream |
