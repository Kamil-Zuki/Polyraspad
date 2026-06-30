# Введение

Методы данной группы управляют structured JSON-артефактами, привязанными к agent run и thread. Используются для сохранения card drafts, mining payloads и других machine-readable результатов, которые UI или downstream-сервисы могут загрузить отдельно от текста assistant message.

**SR группы:** SR-AGENT-ART-01, SR-AGENT-ART-02. Сущность: `agent_artifacts`.

Реализация: `AgentGrpcService` → `AgentThreadService`.

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AGENT-ART-01 | `CreateArtifact` | Unary | Создание JSON payload для run+thread. |
| SR-AGENT-ART-02 | `ListArtifacts` | Unary | Список артефактов треда; filter по run_id. |

---

<span id="grpc-CreateArtifact"></span>

# SR-AGENT-ART-01: Создание артефакта: CreateArtifact

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/08 - Артефакты (Artifacts)#SR-AGENT-ART-01]]

**REST-паритет:** `POST /api/agent/threads/{threadId}/artifacts` (Aggregator).

| Сигнатура | `rpc CreateArtifact(CreateAgentArtifactRequest) returns (AgentArtifactItem)` |
| :--- | :--- |
| **Сообщение запроса** | `CreateAgentArtifactRequest` — thread_id, run_id, kind, payload_json |
| **Сообщение ответа** | `AgentArtifactItem` — id, run_id, thread_id, kind, payload_json, created_at |

## Логика обработки запроса

1. Извлечь `user_id` из metadata; FluentValidation; parse `thread_id`, `run_id`.
2. SELECT thread WHERE `id`, `user_id` — если null → `NOT_FOUND`.
3. EXISTS run WHERE `id = run_id AND thread_id` — если false → `NOT_FOUND`.
4. INSERT `agent_artifacts` с новым Guid, kind, payload_json, UTC created_at.
5. Вернуть `AgentArtifactItem`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидные Guid, пустой kind/payload, validation fail. |
| **UNAUTHENTICATED** | Нет `user_id` в metadata. |
| **NOT_FOUND** | Тред или run не найден / не связаны. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

<span id="grpc-ListArtifacts"></span>

# SR-AGENT-ART-02: Список артефактов: ListArtifacts

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/08 - Артефакты (Artifacts)#SR-AGENT-ART-02]]

**REST-паритет:** `GET /api/agent/threads/{threadId}/artifacts?runId=…` (optional filter).

| Сигнатура | `rpc ListArtifacts(ListAgentArtifactsRequest) returns (ListAgentArtifactsResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ListAgentArtifactsRequest` — thread_id, optional run_id (StringValue) |
| **Сообщение ответа** | `ListAgentArtifactsResponse` — `items[]` типа `AgentArtifactItem` |

## Логика обработки запроса

1. Извлечь `user_id`; FluentValidation; parse `thread_id`.
2. Optional parse `run_id` если StringValue задан и не пуст.
3. Проверить ownership треда; если нет — вернуть пустой список (не ошибка).
4. SELECT `agent_artifacts` WHERE `thread_id`, optional `run_id`, ORDER BY `created_at` DESC.
5. Вернуть `ListAgentArtifactsResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный `thread_id` или `run_id`. |
| **UNAUTHENTICATED** | Нет `user_id` в metadata. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

*Платформенные контракты без RPC: [[00 - gRPC - Общая информация#5. Платформенные контракты (Operations)]].*
