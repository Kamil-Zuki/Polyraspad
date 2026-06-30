# Группа 4: Платформенные контракты (Operations)

## Введение

Эксплуатационные возможности authorization-module: **liveness**, **CORS**, **production configuration guard**, **автоматические миграции** и **Swagger в Development**.

**Метафора:**

Представьте **технический паспорт здания**: датчик «здание стоит» на входе, правила кто может подойти к крыльцу (CORS), и проверка перед открытием, что все системы (JWT, почта, ссылки) настроены по-настоящему, а не placeholder.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к платформенным контрактам.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AUTHMOD-OPS-01** | **Health check (liveness):** GET `/healthz` → `{ "status": "ok" }` без deep checks. |
| **SR-AUTHMOD-OPS-02** | **CORS policy:** Explicit origins, AllowCredentials; wildcard запрещён в prod validation. |
| **SR-AUTHMOD-OPS-03** | **Production startup validation:** JWT, ConfirmationLink, Email SMTP, CORS — fail-fast. |
| **SR-AUTHMOD-OPS-04** | **EF Core migrations at startup:** `db.Database.Migrate()` после legacy baseline helper. |

---

# Детальная спецификация требований

## SR-AUTHMOD-OPS-01: Health check {#SR-AUTHMOD-OPS-01}

Liveness endpoint для orchestrator и deploy scripts.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Liveness only** | No PostgreSQL/SMTP ping in handler. |
| **Route** | `GET /healthz` mapped in Program.cs. |
| **JSON body** | `{ "status": "ok" }`. |

### 2. Высокоуровневое описание

Представим healthz как **зелёную лампочку «процесс жив» на входе в здание auth-module** — датчик не проверяет каждый кабель, только то, что Kestrel принимает соединения.

1. **Request (Опрос orchestrator):** nginx, Kubernetes probe или deploy script периодически вызывает `GET /healthz` (mapped in `Program.cs`).
2. **Handler (Minimal endpoint):** Kestrel отвечает без PostgreSQL ping и без SMTP ping — transient postgres blip не должен убивать pod.
3. **Response (JSON body):** HTTP 200, `{ "status": "ok" }` — единственный контракт liveness.
4. **Scope (Liveness only):** endpoint подтверждает, что ASP.NET process принимает HTTP; deep dependency checks — out of scope.
5. **Contrast (Readiness):** отсутствие проверки БД означает, что healthz **не** гарантирует готовность к `RegisterUser` или SMTP.

Таким образом, **healthz ≠ readiness** для auth-module в текущей реализации.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Deploy verify (Happy Path)

1. **GET** `/healthz`.
2. **Ответ:** HTTP 200, `{ "status": "ok" }`.

---

## SR-AUTHMOD-OPS-02: CORS {#SR-AUTHMOD-OPS-02}

Browser clients (frontend, aggregator dev) обращаются к auth REST с explicit origins.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Config keys** | `Cors:AllowedOrigins` (comma-separated) or legacy `Cors:Urls[]`. |
| **Defaults dev** | `http://localhost:3000`, `http://localhost:5000`. |
| **AllowCredentials** | Enabled on policy `cors`. |
| **No wildcard prod** | `*` rejected in production validation. |

### 2. Высокоуровневое описание

Представим CORS как **список гостей у крыльца REST legacy API** — browser-клиенты (frontend, aggregator dev) могут подойти только с разрешённых адресов.

1. **Parse origins (Список гостей):** at startup из `Cors:AllowedOrigins` (comma-separated) или legacy `Cors:Urls[]`; defaults dev: `http://localhost:3000`, `http://localhost:5000`.
2. **Register policy (Правила крыльца):** policy `cors` с `WithOrigins` + `AllowAnyHeader/Method` + `AllowCredentials` — credentials required для cookie/JWT flows.
3. **Middleware order (Очередность):** `app.UseCors("cors")` before auth middleware — preflight OPTIONS обрабатывается до JWT validation.
4. **Browser preflight (Проверка на входе):** trusted origin → `Access-Control-Allow-Origin` (specific, not `*`) + `Allow-Credentials`; untrusted → browser blocks.
5. **Prod guard (Wildcard запрет):** `*` rejected в production validation (SR-AUTHMOD-OPS-03); gRPC h2c внутри Docker network CORS **не использует**.

Таким образом, CORS защищает **browser surface** REST legacy; gRPC h2c внутри Docker network CORS не использует.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Allowed origin (Happy Path)

1. Browser preflight from `http://localhost:3000`.
2. **Response:** CORS headers allow request.

#### Сценарий Б: Unknown origin (Negative Path)

1. Request from untrusted origin.
2. Browser blocks response (CORS failure).

---

## SR-AUTHMOD-OPS-03: Production startup validation {#SR-AUTHMOD-OPS-03}

Fail-fast guard против placeholder config в Production.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Dev skip** | Validation skipped in Development. |
| **Placeholder detection** | `change-me`, `example`, `yourdomain`, `yoursecretkeyhere`. |
| **ConfirmationLink** | Absolute http/https URL for email template. |
| **Email block** | Host, Port, UserName, Password, Address, DisplayName required. |
| **JWT block** | Secret ≥ 32 chars, Issuer, Audience. |

### 2. Высокоуровневое описание

Представим production validation как **предпусковой чеклист перед открытием auth-module** — JWT, почта и ссылки подтверждения должны быть настроены по-настоящему, а не placeholder.

1. **Dev skip (Локальная разработка):** validation **пропускается** в `Development` environment — Swagger и placeholder config допустимы.
2. **JWT block (Общий секрет):** `Jwt:Secret` ≥ 32 chars, `Jwt:Issuer`, `Jwt:Audience` required — shared with Aggregator для локальной валидации access tokens.
3. **ConfirmationLink (Ссылка в письме):** absolute http/https URL для email template (SR-AUTHMOD-REG-01); placeholder `yourdomain` → fail.
4. **Email SMTP block (Почтовый сервер):** Host, Port, UserName, Password, Address, DisplayName required — без SMTP register flow (SR-AUTHMOD-REG-01) не работает.
5. **CORS block (Origins):** explicit origins required; wildcard `*` rejected; placeholder detection: `change-me`, `example`, `yourdomain`, `yoursecretkeyhere`.
6. **Fail-fast (Не стартовать):** `ValidateAuthorizationConfiguration` собирает errors → `InvalidOperationException` до `app.Run()` — процесс не слушает порт с небезопасным конфигом.

Таким образом, **misconfiguration обнаруживается at boot**, а не при первом register/login в prod.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Misconfigured production (Negative Path)

1. **Startup:** `Jwt:Secret` = `change-me-...`.
2. **Result:** `InvalidOperationException`; process exits.

#### Сценарий Б: Valid production config (Happy Path)

1. All required keys set with real values.
2. **Result:** app starts, migrations run.

---

## SR-AUTHMOD-OPS-04: EF migrations at startup {#SR-AUTHMOD-OPS-04}

Автоматическое применение EF Core migrations при старте контейнера.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Legacy baseline** | `LegacyIdentityDatabaseBaseline.EnsureBaselineBeforeMigrate` before Migrate. |
| **Docker compose** | Runs after postgres healthy. |
| **Database** | Connection string `Db` → PostgreSQL `auth-module`. |
| **Scope** | Identity + RefreshTokens schema. |

### 2. Высокоуровневое описание

Представим startup migrations как **автоматическую раскладку схемы БД при включении контейнера** — Identity tables и RefreshTokens готовы до первого gRPC call.

1. **Docker timing (Ожидание postgres):** container starts after postgres healthy in docker compose; connection string `Db` → PostgreSQL database `auth-module`.
2. **DI scope (Подключение):** create scope, resolve `DataContext` EF Core context at startup.
3. **Legacy baseline (Старые БД):** `LegacyIdentityDatabaseBaseline.EnsureBaselineBeforeMigrate` before `Migrate()` — совместимость с legacy identity databases.
4. **Apply migrations (Схема):** `db.Database.Migrate()` — AspNetUsers (ASP.NET Core Identity) + RefreshTokens schema.
5. **Ready state (Первый register):** после успешного migrate gRPC `RegisterUser` может создавать users без ручного `dotnet ef database update`.

Таким образом, local/docker deploy **не требует ручного dotnet ef database update** — удобно для dev, требует review migrations для prod.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: First container start (Happy Path)

1. Empty postgres database `auth-module`.
2. **Startup:** migrations applied; AspNetUsers + RefreshTokens exist.
3. **Result:** gRPC RegisterUser succeeds.

---

*Конец групп функциональной спецификации Authorization Module.*
