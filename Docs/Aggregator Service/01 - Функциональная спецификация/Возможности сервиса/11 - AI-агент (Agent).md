# Группа 11: AI-агент (Agent)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **AgentService** — **threads** (чат-сессии AI assistant в контексте project), messages, execute run, archive.

AgentService (:5131) orchestrates LLM + tools (vocabulary lookups и т.д.). Aggregator **не** вызывает OpenAI напрямую для agent runs — только gRPC bridge.

**Метафора:**

Представьте **reception AI-консультанта в project**. Вы показываете пропуск (JWT); receptionist (Aggregator) заводит «дело» (thread), принимает вопросы и передаёт их в отдел экспертов (AgentService), который ведёт диалог и вызывает tools.

REST-контракты: `04/.../REST API/` (Agent — при расширении 04).

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к AI agent threads.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-AGENT-01** | **Жизненный цикл AI-тредов:** Список, создание, сообщения, run и archive assistant threads в контексте project через AgentService. |

---

# Детальная спецификация требований

## SR-AGG-AGENT-01: Threads lifecycle {#SR-AGG-AGENT-01}

Полный flow agent threads, привязанных к `projectId`: sidebar history, chat, run execution, archive.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **projectId required** | List/create: empty projectId → HTTP **400** без gRPC. |
| **AgentService gRPC** | `IAgentServiceClient` — port 5131, h2c internal. |
| **JWT + metadata** | `user_id`, `roles` в gRPC headers на каждый call. |
| **Routes** | GET/POST `/api/agent/threads`, GET thread/messages, POST runs, POST archive. |
| **AutoMapper** | protobuf ↔ `AgentThreadDto`, message DTOs. |
| **Error mapping** | RpcException → 401/403/404/502 по StatusCode. |

### 2. Высокоуровневое описание

Представим agent thread как **ticket в support desk**.

1. **List/Create:** пользователь открывает Agent sidebar — видит past threads или создаёт новый для project.
2. **Get/Messages:** загрузка thread metadata и message history для render chat UI.
3. **Execute run:** пользователь отправляет prompt — AgentService runs LLM loop + registered tools.
4. **Archive:** пользователь закрывает thread — soft archive в AgentService, не delete на BFF.

Aggregator stateless между runs — каждый execute run = fresh gRPC round-trip.

Таким образом, **agent intelligence** живёт в AgentService; BFF — auth + JSON ergonomics для frontend.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Инициатор:** Frontend Agent panel (`polyraspad-frontend`).
* **Base:** `/api/agent/threads`.
* **Downstream:** `ListAgentThreads`, `CreateAgentThread`, `GetAgentThread`, `ListAgentMessages`, `ExecuteAgentRun`, `ArchiveAgentThread`.

#### Сценарий А: Новый thread (Happy Path)

**Сценарий:** Пользователь начинает новый чат с assistant в project.

1. **POST** `/api/agent/threads`, body `CreateAgentThreadRequestDto` с `projectId`.
2. **Identity + gRPC:** metadata → `CreateAgentThread`.
3. **Ответ:** HTTP **201**, `AgentThreadDto`.

#### Сценарий Б: Load message history (Happy Path)

1. **GET** `/api/agent/threads/{threadId}/messages`.
2. **gRPC:** list messages.
3. **Ответ:** HTTP **200**, ordered messages DTO.

#### Сценарий В: Execute run (Happy Path)

**Сценарий:** Пользователь отправляет вопрос «объясни эту карточку».

1. **POST** `/api/agent/threads/{threadId}/runs` с user message payload.
2. **gRPC:** execute run — AgentService calls tools if needed.
3. **Ответ:** HTTP **200**, assistant message / run result DTO.

#### Сценарий Г: Archive thread (Happy Path)

1. **POST** `/api/agent/threads/{threadId}/archive`.
2. **Ответ:** HTTP **200** или **204**.

#### Сценарий Д: Missing projectId (Negative Path)

1. **GET** `/api/agent/threads` без query projectId.
2. **BFF validation:** HTTP **400** `{ "error": "projectId is required" }`.

#### Сценарий Е: Thread not found (Negative Path)

1. **GET** `/api/agent/threads/{unknownId}`.
2. **gRPC:** `NotFound` → HTTP **404**.

---

## SR-AGG-AGENT-02: Persist run {#SR-AGG-AGENT-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| Persist without full execute | `POST /api/agent/threads/{id}/runs/persist` |
| Thin BFF | Тело проксируется в AgentService PersistRun |

### 2. Высокоуровневое описание
Клиент может сохранить уже собранный результат run (messages/tool calls/domain decision) без повторного ExecuteRun.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Happy Path
1. POST persist с payload run.
2. AgentService сохраняет AgentRun + связанные записи.
3. HTTP 200/201.

---

*Следующая группа: [[12 - AI-прокси (AI Proxy)]].*
