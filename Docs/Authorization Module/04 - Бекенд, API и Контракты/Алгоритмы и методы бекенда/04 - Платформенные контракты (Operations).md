# Введение

Cross-cutting алгоритмы boot и browser policy для authorization-module. HTTP liveness — [[../Методы API/REST API/02 - Платформенные контракты (Operations)]].

Сверено с `authorization-module.API/Program.cs`.

# 1. Список алгоритмов

| Название алгоритма | SR | Краткое описание |
| :--- | :--- | :--- |
| **CORS policy `cors`** | SR-AUTHMOD-OPS-02 | Explicit origins, AllowCredentials, no `*` in prod |
| **Production startup validation** | SR-AUTHMOD-OPS-03 | Fail-fast JWT, SMTP, ConfirmationLink, CORS |
| **EF Core migrations at startup** | SR-AUTHMOD-OPS-04 | Legacy baseline + `Database.Migrate()` |

---

# Алгоритм CORS policy (SR-AUTHMOD-OPS-02)

## Контекст и область применения

Browser clients (frontend, aggregator dev) к legacy REST. gRPC h2c внутри Docker **не** использует CORS.

## Шаги

1. **Parse origins:** `Cors:AllowedOrigins` (comma-separated) или legacy `Cors:Urls[]`; default dev: `http://localhost:3000`, `http://localhost:5000`.
2. **Register policy `cors`:** `WithOrigins` + `AllowAnyHeader/Method` + `AllowCredentials`.
3. **Pipeline:** `app.UseCors("cors")` после exception middleware, до authentication.
4. **Prod guard:** wildcard `*` rejected в `ValidateCorsConfiguration` (часть OPS-03).

---

# Алгоритм Production startup validation (SR-AUTHMOD-OPS-03)

## Контекст и область применения

`ValidateAuthorizationConfiguration` вызывается **до** `builder.Build()`; в **Development** validation пропускается.

## Проверки (non-Development)

| Блок | Ключи / правило |
| :--- | :--- |
| JWT | `Jwt:Secret` ≥ 32 chars, не placeholder; `Jwt:Issuer`, `Jwt:Audience` |
| ConfirmationLink | absolute http/https URL для email confirm |
| Email SMTP | `Email:Host`, `Port`, `UserName`, `Password`, `Address`, `DisplayName` |
| CORS | ≥1 origin; `*` запрещён |

Placeholder detection: `change-me`, `example`, `yourdomain`, `yoursecretkeyhere`.

## Результат

`InvalidOperationException` с списком ошибок → процесс не стартует.

---

# Алгоритм EF migrations at startup (SR-AUTHMOD-OPS-04)

## Контекст и область применения

Автоматическое применение схемы `auth-module` при старте контейнера (после healthy postgres).

## Шаги

1. `using` scope → resolve `DataContext`.
2. `LegacyIdentityDatabaseBaseline.EnsureBaselineBeforeMigrate(db)` — совместимость legacy Identity DB.
3. `db.Database.Migrate()` — AspNetUsers + RefreshTokens.
4. После успеха — `MapGrpcService<AuthService>`, `MapControllers`, `MapGet("/healthz")`.

## Псевдокод

```csharp
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DataContext>();
LegacyIdentityDatabaseBaseline.EnsureBaselineBeforeMigrate(db);
db.Database.Migrate();
```
