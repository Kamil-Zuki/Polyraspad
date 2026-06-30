# REST API — Аутентификация (Legacy REST)

## Введение

`AccountsController` (`api/v1/auth`) — thin REST wrapper над `IAuthService` для **прямого** доступа к authorization-module (dev / legacy).

**Публичный BFF Polyraspad:** `AggregatorService/Controllers/AuthController` (`api/auth/*`) — основной путь для браузера. Маршруты 1:1 с legacy, плюс **`PUT /api/auth/avatar-url`** (gRPC `UpdateAvatarUrl`).

Validation: FluentValidation на register/login.

### Не реализовано в legacy REST

| SR | gRPC | Legacy REST | BFF Aggregator |
| :--- | :--- | :--- | :--- |
| SR-AUTHMOD-PROF-04 | `UpdateAvatarUrl` | ❌ нет route | ✅ `PUT /api/auth/avatar-url` |

---

# 1. Endpoints

<span id="rest-register"></span>

## POST /api/v1/auth/register

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-REG-01 |
| **gRPC** | [[#grpc-RegisterUser]] |
| **Body** | UserRegistrationRequest JSON |
| **Success** | 201 Created `{ "message": "..." }` |
| **Errors** | 400 validation / domain |

---

<span id="rest-login"></span>

## POST /api/v1/auth/login

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-AUTH-01 |
| **gRPC** | [[#grpc-LoginUser]] |
| **Success** | 200 TokenDto |

---

<span id="rest-refresh"></span>

## POST /api/v1/auth/refresh-token

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-AUTH-02 |
| **gRPC** | [[#grpc-RefreshToken]] |

---

<span id="rest-confirm-email"></span>

## GET /api/v1/auth/confirm-email

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-REG-02 |
| **Query** | userId, token |

---

<span id="rest-me"></span>

## GET /api/v1/auth/me

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-PROF-01 |
| **Auth** | Bearer JWT |
| **Success** | 200 UserInfoDto |

---

<span id="rest-logout"></span>

## POST /api/v1/auth/logout

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-AUTH-03 |
| **Body** | LogoutRequest { refreshToken } |

---

<span id="rest-username"></span>

## PUT /api/v1/auth/username

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-PROF-02 |

---

<span id="rest-password"></span>

## PUT /api/v1/auth/password

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-PROF-03 |

---

<span id="rest-avatar-out-of-scope"></span>

## Avatar URL — out of scope для legacy REST

| | |
| :--- | :--- |
| **SR** | SR-AUTHMOD-PROF-04 |
| **gRPC** | [[../gRPC/03 - Управление профилем (Profile Management)#grpc-UpdateAvatarUrl]] |
| **Legacy** | Не реализован в `AccountsController` |
| **BFF** | `PUT /api/auth/avatar-url` на [[Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/01 - Аутентификация и профиль (Auth)]] |
