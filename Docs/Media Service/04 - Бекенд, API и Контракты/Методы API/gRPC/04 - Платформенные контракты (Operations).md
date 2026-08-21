# Введение

Группа **Operations** документирует контракты Media Service **без RPC** в `media.proto`: HTTP health и обязательные gRPC metadata для library paths.

# 1. Контракты (не-RPC)

| Код требования | Контракт | Описание |
| :------------- | :------- | :------- |
| SR-MEDIA-OPS-01 | `GET /healthz` | Liveness на порту 5121 |
| SR-MEDIA-OPS-02 | Metadata `user_id` | Owner context для Reader Library RPC |

---

# SR-MEDIA-OPS-01: Health-check {#SR-MEDIA-OPS-01}

## Общая информация

**Источник требования:** [[../../../01 - Функциональная спецификация/Возможности сервиса/04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-01]]

| Компонент | Значение |
| :--- | :--- |
| **Endpoint** | `GET /healthz` |
| **Response** | `{ "status": "ok" }` |
| **Порт** | `5121` (shared Kestrel с gRPC h2c) |

Deep check MinIO **не** выполняется — только process up.

---

# SR-MEDIA-OPS-02: gRPC identity context {#SR-MEDIA-OPS-02}

## Общая информация

**Источник требования:** [[../../../01 - Функциональная спецификация/Возможности сервиса/04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-02]]

| Metadata key | Обязательность | Описание |
| :--- | :--- | :--- |
| `user_id` | Да (library RPC) | UUID string owner; из JWT на Aggregator |

Upload/GetUrl RPC **не** требуют metadata — caller identity для binary ops определяется политикой Aggregator (JWT на REST layer).

## Логика

1. Aggregator извлекает `sub` из JWT → gRPC metadata `user_id`.
2. `MediaGrpcService` читает header; missing/invalid → `UNAUTHENTICATED` для library methods.
3. S3 keys scoped: `reader-library/{user_id}/{project_id}/…`.

См. [[../../02 - Архитектура/03 - КАР-3 - gRPC-only API и контекст user_id]].
