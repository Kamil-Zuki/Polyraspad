# Введение

In-app AI assistant threads. JWT. Downstream: **AgentService** (`agent.proto`). Сверено с `AggregatorService/Controllers/AgentController.cs`.

Сообщения пользователя отправляются через **`POST …/runs`** → gRPC `ExecuteRun` (отдельного REST для `CreateRun` / artifacts нет на BFF).

**gRPC-only на AgentService (без REST на Aggregator):** `CreateRun`, `CreateArtifact`, `ListArtifacts` — см. [[../../../../Agent Service/04 - Бекенд, API и Контракты/Методы API/gRPC/00 - gRPC - Общая информация]].

DTO: [[../DTO/05 - Сообщество, биллинг и агент (Community Billing Agent)]].

# 1. Список эндпоинтов

| SR | Method | Route | gRPC (`agent.proto`) |
| :--- | :--- | :--- | :--- |
| SR-AGG-AGENT-01 | GET | `/api/agent/threads?projectId=` | `ListThreads` |
| SR-AGG-AGENT-01 | POST | `/api/agent/threads` | `CreateThread` |
| SR-AGG-AGENT-01 | GET | `/api/agent/threads/{threadId}` | `GetThread` |
| SR-AGG-AGENT-01 | POST | `/api/agent/threads/{threadId}/archive` | `ArchiveThread` |
| SR-AGG-AGENT-01 | GET | `/api/agent/threads/{threadId}/messages` | `ListMessages` |
| SR-AGG-AGENT-01 | POST | `/api/agent/threads/{threadId}/runs` | `ExecuteRun` |

---

# SR-AGG-AGENT-01: List threads: GET /api/agent/threads

## Общая информация

Список активных тредов проекта для sidebar UI.

| Тип метода | GET |
| :--- | :--- |
| **Query** | `projectId` (required, uuid) |
| **DTO успешного ответа** | `AgentThreadListItemDto[]` |

## Логика обработки запроса

* JWT → metadata `user_id`, `roles`
* gRPC [`ListThreads`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Треды%20и%20сообщения%20(Threads).md#grpc-ListThreads)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing projectId |
| **401** | JWT |
| **502** | AgentService |

---

# SR-AGG-AGENT-01: Создание thread: POST /api/agent/threads

## Общая информация

Новый диалог в контексте project.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `CreateAgentThreadRequestDto` (`projectId`) |
| **DTO успешного ответа** | `AgentThreadDto` |

## Логика обработки запроса

* JWT → metadata
* gRPC [`CreateThread`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Треды%20и%20сообщения%20(Threads).md#grpc-CreateThread)

## Успешный ответ

HTTP **201**, `AgentThreadDto`.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Invalid projectId |
| **401** | JWT |
| **502** | AgentService |

---

# SR-AGG-AGENT-01: Execute run: POST /api/agent/threads/{threadId}/runs

## Общая информация

Turn агента: user message → assistant reply (orchestrator persist в одном вызове).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `ExecuteAgentRunRequestDto` (`projectId`, `userText`, optional langs/deck) |
| **DTO успешного ответа** | `CreateAgentRunResponseDto` |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| threadId | string | uuid thread |

## Логика обработки запроса

* Validate `projectId`, `userText` required
* gRPC [`ExecuteRun`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/02%20-%20Запуски%20и%20оркестрация%20(Runs).md#grpc-ExecuteRun)

## Успешный ответ

HTTP **200**, `CreateAgentRunResponseDto`.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing projectId or userText |
| **404** | Thread not found |
| **502** | Downstream |

---

# SR-AGG-AGENT-01: Get thread: GET /api/agent/threads/{threadId}

## Общая информация

Детали одного треда (incl. `archivedAt` если archived).

| Тип метода | GET |
| :--- | :--- |
| **DTO успешного ответа** | `AgentThreadDto` |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| threadId | string | uuid thread |

## Логика обработки запроса

* JWT → metadata
* gRPC [`GetThread`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Треды%20и%20сообщения%20(Threads).md#grpc-GetThread)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Thread not found |
| **401** | JWT |
| **502** | AgentService |

---

# SR-AGG-AGENT-01: List messages: GET /api/agent/threads/{threadId}/messages

## Общая информация

Cursor-based история сообщений треда.

| Тип метода | GET |
| :--- | :--- |
| **Query** | `limit` (default 100, max 100), optional `before` (message id) |
| **DTO успешного ответа** | `AgentMessageListDto` (`items`, `nextBefore`) |

## Логика обработки запроса

* JWT → metadata
* gRPC [`ListMessages`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Треды%20и%20сообщения%20(Threads).md#grpc-ListMessages)
* Сообщения в хронологическом порядке; `nextBefore` для предыдущей страницы

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Thread / cursor message |
| **400** | Invalid limit |
| **502** | AgentService |

---

# SR-AGG-AGENT-01: Archive thread: POST /api/agent/threads/{threadId}/archive

## Общая информация

Soft archive thread (не HTTP DELETE).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | N/A |

## Логика обработки запроса

* gRPC [`ArchiveThread`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Треды%20и%20сообщения%20(Threads).md#grpc-ArchiveThread)
* HTTP **204 No Content**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Thread not found |
| **502** | Downstream |

---

# SR-AGG-AGENT-01: Get thread: GET /api/agent/threads/{threadId}

## Общая информация

Детали одного треда (incl. `archivedAt` если archived).

| Тип метода | GET |
| :--- | :--- |
| **DTO успешного ответа** | `AgentThreadDto` |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| threadId | string | uuid thread |

## Логика обработки запроса

* JWT → metadata
* gRPC [`GetThread`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Треды%20и%20сообщения%20(Threads).md#grpc-GetThread)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Thread not found |
| **401** | JWT |
| **502** | AgentService |

---

# SR-AGG-AGENT-01: List messages: GET /api/agent/threads/{threadId}/messages

## Общая информация

Cursor-based история сообщений треда.

| Тип метода | GET |
| :--- | :--- |
| **Query** | `limit` (default 100, max 100), optional `before` (message id) |
| **DTO успешного ответа** | `AgentMessageListDto` (`items`, `nextBefore`) |

## Логика обработки запроса

* JWT → metadata
* gRPC [`ListMessages`](../../../../Agent%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Треды%20и%20сообщения%20(Threads).md#grpc-ListMessages)
* Сообщения в хронологическом порядке; `nextBefore` для предыдущей страницы

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Thread / cursor message |
| **400** | Invalid limit |
| **502** | AgentService |
