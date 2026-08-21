# Введение

Методы группы «Треды и сообщения» — CRUD диалогов Agent в контексте project и чтение истории сообщений.

Реализация: `AgentGrpcService` + `AgentThreadService`. Metadata: `user_id`, `roles` из gRPC context.

**SR группы:** SR-AGENT-THREAD-01 … SR-AGENT-THREAD-04, SR-AGENT-MSG-01.

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AGENT-THREAD-01 | `ListThreads` | Unary | Активные (не archived) треды project (опциональная фильтрация по `agent_id`) |
| SR-AGENT-THREAD-02 | `CreateThread` | Unary | Новый thread в project с поддержкой `agent_id` и `system_prompt_override` |
| SR-AGENT-THREAD-03 | `GetThread` | Unary | Thread по id incl. `archived_at` |
| SR-AGENT-MSG-01 | `ListMessages` | Unary | Cursor-paginated messages |
| SR-AGENT-THREAD-04 | `ArchiveThread` | Unary | Soft-archive thread |

---

<span id="grpc-ListThreads"></span>

# SR-AGENT-THREAD-01: List threads: ListThreads

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Управление тредами (Thread Management)#SR-AGENT-THREAD-01]]

Sidebar списка диалогов в project. Archived threads исключаются.

| Сигнатура | `rpc ListThreads(ListAgentThreadsRequest) returns (ListAgentThreadsResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id`, `project_id`, `agent_id` (опциональный фильтр по персоне агента) |
| **Сообщение ответа** | `items[]` — `AgentThreadListItem` (включая `agent_id`) |

## Логика обработки запроса

1. `userId = GrpcContextHelper.GetUserId(context)`; FluentValidation request.
2. Parse `project_id` → GUID; иначе **INVALID_ARGUMENT**.
3. `AgentThreadService.ListThreadsAsync(userId, projectId, agentId, roles)` — Vocabulary project access gate + фильтрация.
4. Map entities → proto `items`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Validation / invalid project UUID |
| **NOT_FOUND** | Project not found / no access |
| **INTERNAL** | Unhandled |

---

<span id="grpc-CreateThread"></span>

# SR-AGENT-THREAD-02: Create thread: CreateThread

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Управление тредами (Thread Management)#SR-AGENT-THREAD-02]]

| Сигнатура | `rpc CreateThread(CreateAgentThreadRequest) returns (AgentThreadResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id`, `project_id`, `agent_id` (persona ID), `system_prompt_override` (опциональный кастомный промпт) |
| **Сообщение ответа** | `AgentThreadResponse` (включая `agent_id`, title auto-derived on first run) |

## Логика обработки запроса

1. Validate + parse UUIDs.
2. `CreateThreadAsync` — ensure project access; insert thread row with `AgentId` and `SystemPromptOverride`.
3. Return mapped `AgentThreadResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Validation |
| **NOT_FOUND** | Project |
| **INTERNAL** | Unhandled |

---

<span id="grpc-GetThread"></span>

# SR-AGENT-THREAD-03: Get thread: GetThread

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Управление тредами (Thread Management)#SR-AGENT-THREAD-03]]

| Сигнатура | `rpc GetThread(GetAgentThreadRequest) returns (AgentThreadResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id`, `thread_id` |
| **Сообщение ответа** | Thread incl. optional `archived_at` and `agent_id` |

## Логика обработки запроса

1. Validate; parse `thread_id` UUID.
2. `GetThreadAsync(userId, threadId)` — owner/access check.
3. Return `AgentThreadResponse`.

---

<span id="grpc-ListMessages"></span>

# SR-AGENT-MSG-01: List messages: ListMessages

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - История сообщений (Message History)#SR-AGENT-MSG-01]]

| Сигнатура | `rpc ListMessages(ListAgentMessagesRequest) returns (ListAgentMessagesResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id`, `thread_id`, `limit` (max 100), `before` (cursor UUID) |
| **Сообщение ответа** | `items[]` — `AgentMessageItem`, `next_before` cursor |

---

<span id="grpc-ArchiveThread"></span>

# SR-AGENT-THREAD-04: Archive thread: ArchiveThread

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Управление тредами (Thread Management)#SR-AGENT-THREAD-04]]

| Сигнатура | `rpc ArchiveThread(ArchiveAgentThreadRequest) returns (google.protobuf.Empty)` |
| :--- | :--- |
| **Сообщение запроса** | `user_id`, `thread_id` |
| **Сообщение ответа** | `google.protobuf.Empty` |
