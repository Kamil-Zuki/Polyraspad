# Platform Operations

Группа **Operations** — без отдельных RPC в `authorization.proto`. Контракты реализованы в `Program.cs` и startup pipeline.

**SR:** SR-AUTHMOD-OPS-01 … SR-AUTHMOD-OPS-04 ([[../../01 - Функциональная спецификация/Возможности сервиса/04 - Платформенные контракты (Operations)]]).

---

## Вход / выход

| SR | Механизм | Вход | Выход / эффект |
| :--- | :--- | :--- | :--- |
| SR-AUTHMOD-OPS-01 | `GET /healthz` | HTTP GET anonymous | `200` + `{ "status": "ok" }` |
| SR-AUTHMOD-OPS-02 | CORS middleware | `CORS_ALLOWED_ORIGINS` env | Policy с explicit origins, `AllowCredentials` |
| SR-AUTHMOD-OPS-03 | Startup validation | `IHostEnvironment`, options | Fail-fast если JWT/SMTP/confirm link placeholder в Production |
| SR-AUTHMOD-OPS-04 | EF migrations | `ApplicationDbContext` | `Database.Migrate()` at startup |

---

## Псевдокод (startup)

```
WebApplication.Build()
ConfigureServices: Identity, JWT, gRPC, SMTP, CORS, FluentValidation
if (IsProduction) ValidateProductionOptions() // OPS-03
app.MapGrpcService<AuthService>()
app.MapControllers() // legacy REST AccountsController
app.MapGet("/healthz", () => Ok({ status: "ok" })) // OPS-01
using scope → db.Database.Migrate() // OPS-04
app.Run()
```

---

## Связанные артефакты

| Файл | Роль |
| :--- | :--- |
| `authorization-module.API/Program.cs` | healthz, migrations, gRPC map |
| `authorization-module.API/Options/*` | JWT, SMTP, CORS validation |
| Swagger | Development only (`MapSwagger`) |

---

## gRPC vs HTTP

| Контракт | Порт | Протокол |
| :--- | :--- | :--- |
| `AuthService` gRPC | `5027` | HTTP/2 (h2c) |
| Legacy REST + healthz | `5027` | HTTP/1.1 same Kestrel |

Публичный браузерный трафик — **Aggregator** (`api/auth/*`), не прямой вызов authorization-module REST в production.
