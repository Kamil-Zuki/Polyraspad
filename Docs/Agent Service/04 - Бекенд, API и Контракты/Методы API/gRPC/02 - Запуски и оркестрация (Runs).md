# Введение

Методы группы «Запуски и оркестрация» — persist run payload и server-side pipeline `ExecuteRun`.

**SR группы:** **SR-AGENT-RUN-01**, **SR-AGENT-RUN-02**. Алгоритм: [[../../Алгоритмы и методы бекенда/02 - Оркестрация ExecuteRun|ExecuteRun orchestration]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AGENT-RUN-01 | `CreateRun` | Unary | Persist готового run (messages, domain, tools) |
| SR-AGENT-RUN-02 | `ExecuteRun` | Unary | Classify → route → tool → CreateRun (включая `first_deck_id` и `is_initial_greeting`) |

---

<span id="grpc-CreateRun"></span>

# SR-AGENT-RUN-01: Persist run: CreateRun

## Общая информация

**Источник требования:** [[03 - Запуски агента (Agent Runs)#SR-AGENT-RUN-01]]

Атомарная запись user/assistant messages, run row, domain decision, tool calls. Используется `ExecuteRun` и потенциально client-driven flows.

| Сигнатура | `rpc CreateRun(CreateAgentRunRequest) returns (CreateAgentRunResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `thread_id`, `project_id`, `user_message`, `assistant_message`, `domain_decision`, `tool_calls[]`, optional `model` |
| **Сообщение ответа** | `run`, `user_message`, `assistant_message` |

## Логика обработки запроса

1. `GrpcContextHelper.GetUserId`; FluentValidation.
2. Parse thread/project UUIDs.
3. Map → `CreateAgentRunDto`; `AgentThreadService.CreateRunAsync` (single DB transaction).
4. Reject archived thread → **FAILED_PRECONDITION**.
5. Map result → `CreateAgentRunResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Validation / bad UUID |
| **NOT_FOUND** | Thread |
| **FAILED_PRECONDITION** | Thread archived |
| **INTERNAL** | Unhandled |

---

<span id="grpc-ExecuteRun"></span>

# SR-AGENT-RUN-02: Orchestrate run: ExecuteRun

## Общая информация

**Источник требования:** [[03 - Запуски агента (Agent Runs)#SR-AGENT-RUN-02]]

Server-side turn: domain classify → intent route → tool execution → persist via CreateRun.

| Сигнатура | `rpc ExecuteRun(ExecuteAgentRunRequest) returns (CreateAgentRunResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `thread_id`, `project_id`, `user_text`, optional `source_lang`, `target_lang`, `first_deck_id`, `is_initial_greeting` |
| **Сообщение ответа** | `CreateAgentRunResponse` (run, user_message, assistant_message) |

## Логика обработки запроса

1. Validate request; parse UUIDs; read roles from metadata.
2. `AgentOrchestrator.ExecuteRunAsync` — см. [[../../Алгоритмы и методы бекенда/02 - Оркестрация ExecuteRun]].
3. Pipeline: EnsureProjectAccess → domain gate → intent router → tool → CreateRun persist.
4. Map → `CreateAgentRunResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing user_text / validation |
| **NOT_FOUND** | Thread / project |
| **FAILED_PRECONDITION** | Archived thread / domain blocked |
| **INTERNAL** | Unhandled |
