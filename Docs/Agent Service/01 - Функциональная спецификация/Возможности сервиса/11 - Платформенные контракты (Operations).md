# Группа 11: Платформенные контракты (Operations)

## Введение

Эксплуатационные контракты Agent Service: liveness, schema migrations, transport (gRPC-only).

**Метафора:** платформенные контракты — **технический паспорт здания**. Health-check, миграции и единый gRPC-вход — чтобы оркестратор и CI знают, что сервис жив и готов принимать вызовы.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Платформенные контракты (Operations).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-OPS-01** | **Health check:** Minimal liveness endpoint. |
| **SR-AGENT-OPS-02** | **Startup migrations:** EF Migrate on boot. |
| **SR-AGENT-OPS-03** | **gRPC-only Kestrel:** HTTP/2 listener 5131. |

---

# Детальная спецификация требований

## SR-AGENT-OPS-01: Health check {#SR-AGENT-OPS-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Endpoint** | GET `/healthz` → 200 JSON `{ status: "ok" }`. |
| **No deps** | Не проверяет Postgres/Vocabulary/LLM. |

### 2. Высокоуровневое описание

Представим health check как **индикатор «здание включено» на техническом паспорте**.

1. **Endpoint:** GET `/healthz` → HTTP 200 JSON `{ status: "ok" }`.
2. **Minimal liveness:** не проверяет Postgres, Vocabulary gRPC или LLM provider.
3. **Consumers:** Docker Compose restart policy и CI pipeline используют endpoint как process-alive signal.

Таким образом, platform знает, что Kestrel process принимает HTTP; readiness с dependency checks — отдельная concern.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Container liveness (Happy Path)

1. Orchestrator GET `/healthz`.
2. HTTP 200 `{ "status": "ok" }`.

---

## SR-AGENT-OPS-02: Startup migrations {#SR-AGENT-OPS-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Scope** | Schema `internal`, history table `__EFMigrationsHistory`. |
| **Docker** | Migrate в `Program.cs` before MapGrpcService. |

### 2. Высокоуровневое описание

Представим startup migrations как **автоматическую подготовку схемы до открытия gRPC-дверей**.

1. **Boot hook:** `Program.cs` вызывает `db.Database.Migrate()` before `MapGrpcService`.
2. **Schema `internal`:** EF history table `__EFMigrationsHistory` в dedicated schema.
3. **Pending apply:** новые migration files создают/обновляют `agent_*` tables на first deploy или upgrade.
4. **Block until ready:** gRPC server не принимает traffic, пока migrate не завершится успешно.

Таким образом, контейнер сам доводит PostgreSQL schema до актуальной версии перед приёмом PolyGuide calls.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: First deploy (Happy Path)

1. Новый образ с migration `InitAgentServiceTables`.
2. Startup `db.Database.Migrate()` создаёт таблицы agent_*.
3. gRPC server starts.

---

## SR-AGENT-OPS-03: gRPC-only Kestrel {#SR-AGENT-OPS-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Protocol** | HttpProtocols.Http2 only on 5131. |
| **Container bind** | `0.0.0.0` in container; Loopback local dev. |
| **h2c** | Plaintext HTTP/2 inside Docker network. |

### 2. Высокоуровневое описание

Представим gRPC-only Kestrel как **единственный служебный вход в Agent Service**.

1. **Listener:** HttpProtocols.Http2 only on port 5131; h2c plaintext inside Docker network.
2. **Bind policy:** `0.0.0.0` in container; Loopback for local dev — без public REST surface.
3. **Client boundary:** Aggregator — единственный caller через typed gRPC client; browser traffic не предусмотрен.
4. **Thin BFF topology:** PolyGuide REST живёт на Aggregator; Agent остаётся internal microservice.

Таким образом, все PolyGuide AI flows идут через Aggregator gRPC proxy, сохраняя contract-first service boundaries.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Internal gRPC call (Happy Path)

1. Aggregator opens HTTP/2 channel to `agent-service:5131`.
2. `ExecuteRun` RPC completes over h2c inside Docker network.

---

*Конец групп функциональной спецификации Agent Service.*
