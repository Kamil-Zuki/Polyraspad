# Введение

Группа **Operations** не добавляет proto messages. Контракт identity — gRPC metadata, не JSON body.

# 1. gRPC metadata (не DTO body)

| Header | Тип | SR | Описание |
| :--- | :--- | :--- | :--- |
| `user_id` | UUID string | SR-MEDIA-OPS-02 | Caller user id; mandatory для library RPC |

Aggregator добавляет header в `CallOptions` при создании gRPC client call после JWT validation.

**gRPC:** [[04 - Платформенные контракты (Operations)]]
**Алгоритм:** [[Алгоритмы и методы бекенда/04 - Платформенные контракты (Operations)]]
