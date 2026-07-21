---
description: "[G3 · 04 · RabbitMQ] Message block template, exchange/queue/DLQ"
globs: "**/04 - Бекенд, API и Контракты/**/Работа с Rabbit MQ/**"
alwaysApply: false
---

# Rabbit MQ (`Работа с Rabbit MQ/`)

Async messaging: consume from external IdP, publish platform events, background job queues. Align with `02` КАР (e.g. Back-Channel Logout).

## `00 - Работа с Rabbit MQ - Общая информация.md`

1. `# Введение` — EDA role, SR codes.
2. `# 1. Группы событий и задач` — summary table.
3. `# 2 … N` — per-group: SR | Publish/Consume | Queue/Topic | Description.

## Group file `NN - [Group name].md`

1. `# Введение`
2. `# 1. Список событий` — message table.
3. Each message — block below; separate with `---`.

## Message Block Template

```markdown
# Событие: [Название] ([SR-CODE])

## Общая информация

| Тип операции | Publish \| Consume |
| :--- | :--- |
| **Exchange / Топик** | … |
| **Routing Key** | … |
| **Очередь** | … |
| **DTO сообщения** | `PayloadDto` |

## Параметры сообщения

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| … | … | … |

## Логика обработки запроса

* Idempotency (`eventId` dedup)
* Side effects (Redis, gRPC, WSS publish)
* ACK on success; NACK + requeue / DLQ on failure

## Статус-коды при ошибках

| Код / Тип | Описание | Действие воркера |
| :--- | :--- | :--- |
| **INTERNAL** | DB down | NACK + exponential backoff |
| **INVALID_PAYLOAD** | Missing required field | ACK → DLQ |
```

Durable queues and publisher confirms where enterprise reliability is required (document in logic block).
