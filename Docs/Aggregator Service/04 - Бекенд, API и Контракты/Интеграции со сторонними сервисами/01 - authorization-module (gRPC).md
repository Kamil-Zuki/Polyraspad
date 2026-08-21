# Введение

gRPC-клиент к **authorization-module**. Proto: `authorization.proto`. Config: `AggregatorService:AuthorizationModuleBaseUrl`.

# Общая информация

| Параметр | Значение |
| :--- | :--- |
| **Transport** | h2c (Docker internal) |
| **Auth на вызовах** | JWT validation локально на BFF; gRPC metadata `user_id`, `roles` |
| **SR** | SR-AGG-AUTH-* |

# Используемые gRPC методы

| REST (Aggregator) | gRPC AuthService |
| :--- | :--- |
| POST /api/Auth/register | Register |
| POST /api/Auth/login | Login |
| POST /api/Auth/refresh | RefreshToken |
| POST /api/Auth/confirm | ConfirmEmail |
| GET /api/Auth/me | GetUserInfo |
| POST /api/Auth/logout | Logout |
| PUT profile endpoints | UpdateUsername, UpdatePassword, UpdateAvatar |

# Обработка ошибок

gRPC → HTTP через [[01 - Rate Limiting и gRPC status mapping]]. Public auth routes дополнительно rate-limited (**SR-AGG-AUTH-08**).

# Resilience

Typed `GrpcChannel` + Polly retry на `Unavailable`. Circuit breaker на repeated failures.
