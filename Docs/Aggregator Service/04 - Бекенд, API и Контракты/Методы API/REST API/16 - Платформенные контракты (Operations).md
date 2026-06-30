# Введение

Health, discovery и cross-cutting HTTP контракты Aggregator как public BFF. **Не** дублирует gRPC/Socket/Redis — их нет у сервиса.

# 1. Список эндпоинтов

| SR | Method | Route | Назначение |
| :--- | :--- | :--- | :--- |
| SR-AGG-OPS-01 | GET | `/healthz` | Liveness (process up) |
| SR-AGG-OPS-04 | GET | `/swagger` | OpenAPI UI (**Development** only) |
| SR-AGG-OPS-02 | — | CORS | Default policy `Cors:AllowedOrigins` |

---

# SR-AGG-OPS-01: Health check: GET /healthz

## Общая информация

Kubernetes/Docker liveness. **Не** проверяет все downstream gRPC (deep health — отдельный optional endpoint).

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | `{ "status": "ok" }` |

## Логика обработки запроса

* Anonymous allowed
* HTTP **200** если Kestrel принимает запросы

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **503** | Process shutting down |

---

# SR-AGG-OPS-02: CORS policy

## Общая информация

**Источник требования:** [[../../../01 - Функциональная спецификация/Возможности сервиса/16 - Платформенные контракты (Operations)#SR-AGG-OPS-02]]

Browser credentialed requests (cookies, `Authorization` на media serve-image) требуют **explicit origin**, не `*`.

| Config | `Cors:AllowedOrigins` (comma-separated); default dev `http://localhost:3000` |
| :--- | :--- |
| Policy | Default CORS: `WithOrigins` + `AllowAnyMethod/Header` + `AllowCredentials` |
| Prod guard | `*` rejected в `ValidateAggregatorConfiguration` (SR-AGG-OPS-03) |

## Логика

1. Parse origins at startup → `ValidateCorsConfiguration`.
2. `app.UseCors()` после HTTPS redirect, до rate limiter и auth.
3. Untrusted browser origin → CORS failure на клиенте.

---

# SR-AGG-OPS-04: Swagger UI in Development

## Общая информация

**Источник требования:** [[../../../01 - Функциональная спецификация/Возможности сервиса/16 - Платформенные контракты (Operations)#SR-AGG-OPS-04]]

| Route prefix | `/swagger` |
| :--- | :--- |
| OpenAPI JSON | `/swagger/v1/swagger.json` |
| JWT Bearer | Swagger security definition + `SecurityRequirementsOperationFilter` |

Регистрация только при `app.Environment.IsDevelopment()`. Production: middleware не активен.

---

# Cross-cutting: gRPC status → HTTP

Все REST handlers используют единый mapper (см. [[01 - Rate Limiting и gRPC status mapping]]):

| gRPC | HTTP |
| :--- | :--- |
| InvalidArgument | 400 |
| NotFound | 404 |
| AlreadyExists | 409 |
| PermissionDenied | 403 |
| Unauthenticated | 401 |
| Unavailable / DeadlineExceeded | 502 |
| Unimplemented | 501 |

---

# Cross-cutting: JWT

* Header: `Authorization: Bearer <access_token>`
* Issuer/audience: shared с authorization-module (`JWT_ISSUER`, `JWT_AUDIENCE`)
* Phantom token pattern **не** применяется на Aggregator — валидация JWT локально
