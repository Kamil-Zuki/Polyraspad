# Введение

Эксплуатационные HTTP-контракты authorization-module **без** legacy auth routes: liveness и Swagger UI в Development. CORS, production validation и EF migrations — в [[../../Алгоритмы и методы бекенда/04 - Платформенные контракты (Operations)]].

Сверено с `authorization-module.API/Program.cs`.

# 1. Список эндпоинтов

| SR | Method | Route | Назначение |
| :--- | :--- | :--- | :--- |
| SR-AUTHMOD-OPS-01 | GET | `/healthz` | Liveness JSON `{ "status": "ok" }` |
| — | GET | `/authorization-module/swagger` | Swagger UI (**Development** only) |

---

<span id="rest-healthz"></span>

# SR-AUTHMOD-OPS-01: Health check: GET /healthz

## Общая информация

**Источник требования:** [[../../../01 - Функциональная спецификация/Возможности сервиса/04 - Платформенные контракты (Operations)#SR-AUTHMOD-OPS-01]]

Minimal liveness для Docker Compose и deploy. **Не** проверяет PostgreSQL или SMTP.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | `{ "status": "ok" }` |

## Логика обработки запроса

1. `app.MapGet("/healthz", …)` — anonymous.
2. HTTP **200** и JSON body при работающем Kestrel.
3. Deep checks (postgres, SMTP) **не** выполняются.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **503** | Process shutting down (редко; при остановке host) |

---

# Swagger UI (Development)

## Общая информация

Interactive OpenAPI для legacy REST `/api/v1/auth/*`. Регистрация только при `IsDevelopment()`.

| Route prefix | `authorization-module/swagger` |
| :--- | :--- |
| JSON | `/authorization-module/swagger/v1/swagger.json` |
| JWT authorize | OAuth2 API key scheme `Authorization` |

Production: Swagger middleware **не** регистрируется.
