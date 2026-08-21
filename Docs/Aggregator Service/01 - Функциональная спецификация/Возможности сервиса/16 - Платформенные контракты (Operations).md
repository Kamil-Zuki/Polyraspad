# Группа 16: Платформенные контракты (Operations)

## Введение

В этом разделе описываются **не-domain** контракты Aggregator Service — health check, CORS, production startup validation, Swagger в Development. Реализация в `Program.cs`, не в business controllers.

Эти SR определяют **operability** шлюза: deploy gates, browser security, fail-fast config, developer tooling.

**Метафора:**

Представьте **технический паспорт здания API**: датчик «жив ли сервис», правила пропускного режима (CORS), checklist перед открытием (startup validation) и интерактивная карта для строителей (Swagger только в dev).

Архитектура: [[02 - КАР-6 - Production Configuration Guard]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к платформенным контрактам.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-OPS-01** | **Проверка liveness процесса:** Anonymous GET `/healthz` — process up без deep downstream checks. |
| **SR-AGG-OPS-02** | **Политика CORS для браузера:** Явный список origins и AllowCredentials; wildcard запрещён в Production. |
| **SR-AGG-OPS-03** | **Fail-fast валидация Production-конфига:** Некорректные JWT, CORS, service URLs или dev AI key — старт приложения невозможен. |
| **SR-AGG-OPS-04** | **Swagger UI в Development:** Interactive OpenAPI и JWT Bearer authorize только в dev environment. |

---

# Детальная спецификация требований

## SR-AGG-OPS-01: Health check {#SR-AGG-OPS-01}

Минимальный liveness endpoint для load balancer, Docker и deploy workflow. **Не** проверяет downstream gRPC health.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Anonymous** | No auth on `/healthz`. |
| **Minimal payload** | `{ "status": "ok" }` — process up. |
| **No deep checks** | Vocabulary/Media down не меняет ответ. |
| **Deploy gate** | `.github/workflows/deploy.yml` — GET `/healthz` после deploy. |
| **Implementation** | `app.MapGet("/healthz", …)` в `Program.cs`. |

### 2. Высокоуровневое описание

Представим healthz как **зелёную лампочку «сервер включён»** на щите.

1. **Orchestrator** (nginx, deploy script) периодически GET `/healthz`.
2. **Kestrel** отвечает 200 если ASP.NET process принимает connections.
3. **Deep health** (gRPC ping Vocabulary) — out of scope; при необходимости отдельный endpoint.

Таким образом, healthz отделяет **process liveness** от **dependency readiness**.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** GET `/healthz` (root, не under `/api`).

#### Сценарий А: Deploy verify (Happy Path)

**Сценарий:** GitHub Actions deploy проверяет aggregator после `docker compose up`.

1. **GET** `https://api.polyraspad.online/healthz`.
2. **Ответ:** HTTP **200**, `{ "status": "ok" }`.
3. **Deploy job:** success.

---

## SR-AGG-OPS-02: CORS {#SR-AGG-OPS-02}

Browser clients (app, landing) вызывают API cross-origin. Credentialed requests (Bearer + cookies on media proxy) требуют explicit origins — wildcard `*` запрещён.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Explicit origins** | `CORS_ALLOWED_ORIGINS` env → `Cors:AllowedOrigins`. |
| **AllowCredentials** | Required для credentialed media/auth flows. |
| **No wildcard** | `*` incompatible with credentials — validation rejects in Production. |
| **Preflight** | OPTIONS handled by CORS middleware. |

### 2. Высокоуровневое описание

Представим CORS как **список гостей на вечеринке API**.

1. **Browser** отправляет `Origin: https://app.polyraspad.online`.
2. **CORS middleware** сверяет с allowlist.
3. **Match:** response includes `Access-Control-Allow-Origin` (specific origin, not `*`) + `Allow-Credentials`.
4. **Mismatch:** browser blocks response — frontend видит network error.

Misconfigured CORS ломает login, API calls и serve-image preview.

Таким образом, CORS — **browser security contract**, не замена JWT auth.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Frontend API call (Happy Path)

**Сценарий:** SPA на app domain вызывает aggregator API.

1. **Preflight OPTIONS** с Origin app URL.
2. **CORS:** Allow-Origin = exact app URL.
3. **GET/POST** with credentials succeeds.

#### Сценарий Б: Unknown origin (Negative Path)

1. **Origin** not in allowlist.
2. **Browser:** blocks; no ACAO header.

---

## SR-AGG-OPS-03: Production startup validation {#SR-AGG-OPS-03}

Fail-fast при старте в non-Development: invalid config → process exception, container restart until `.env` fixed.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Development skip** | `ValidateAggregatorConfiguration` no-op in Development. |
| **JWT** | Secret ≥ 32 chars, non-placeholder; Issuer, Audience required. |
| **CORS** | At least one origin; no `*`. |
| **Service URLs** | Vocabulary, Authorization, Media base URLs — valid absolute http(s) URIs. |
| **AI proxy key** | Cannot use dev default `dev-ai-proxy-shared-secret` in Production. |

### 2. Высокоуровневое описание

Представим validation как **checklist перед запуском реактора**.

1. **Host** builds `WebApplication`, reads configuration.
2. **Validator** collects errors (JWT, CORS, URLs, AI key).
3. **Any error:** `InvalidOperationException` with bullet list — Kestrel never listens.
4. **Ops:** fix `.env` / secrets → container healthy start.

Предотвращает silent misconfig (empty JWT secret, missing vocabulary URL) в production.

Таким образом, **unsafe deploy** блокируется at boot, не at first user request.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Missing JWT secret (Negative Path)

**Сценарий:** Production container starts with placeholder Jwt:Secret.

1. **Startup:** `ValidateJwtConfiguration` adds error.
2. **Process:** throws `InvalidOperationException`.
3. **Docker:** restart loop; `/healthz` never reachable.

#### Сценарий Б: Valid production config (Happy Path)

1. **All checks pass.**
2. **Kestrel listens;** `/healthz` returns 200.

---

## SR-AGG-OPS-04: Swagger in Development {#SR-AGG-OPS-04}

Interactive OpenAPI UI для local/dev debugging. **Не** exposed в Production.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Development only** | `UseSwagger` / `UseSwaggerUI` gated by `IsDevelopment()`. |
| **JWT Bearer** | Swagger security scheme для `[Authorize]` endpoints. |
| **Endpoint** | `/swagger`, `/swagger/v1/swagger.json`. |
| **Not in Production** | Public prod surface не exposes Swagger UI. |

### 2. Высокоуровневое описание

Представим Swagger как **интерактивную карту для разработчиков**.

1. **Dev** runs Aggregator locally (`ASPNETCORE_ENVIRONMENT=Development`).
2. **Opens** `/swagger` — all REST controllers listed.
3. **Authorize** with JWT from login flow — test protected endpoints manually.
4. **Production:** middleware not registered — 404 on `/swagger`.

Таким образом, Swagger ускоряет contract exploration без отдельного Postman collection sync.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Local API exploration (Happy Path)

**Сценарий:** Backend dev tests new Cards endpoint.

1. **Environment:** Development.
2. **Navigate** `/swagger` → Authorize Bearer token.
3. **Execute** POST `/api/Cards` — inspect response schema.

#### Сценарий Б: Swagger in Production (Negative Path)

1. **Environment:** Production.
2. **GET** `/swagger` → **404** (middleware not enabled).

---

*Следующая группа: [[17 - Уроки и прогресс (Lessons)]].*
