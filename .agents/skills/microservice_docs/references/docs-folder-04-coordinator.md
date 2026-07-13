---
description: "[G2 · 04 coordinator] Дерево, Contract Layers, alignment, consistency"
globs: "**/04 - Бекенд, API и Контракты/**"
alwaysApply: false
---

# Эталон

Mirror folder tree and naming from:

`(Done) Authorization Service/04 - Бекенд, API и Контракты/`

```
04 - Бекенд, API и Контракты/
├── Методы API/
│   ├── DTO/
│   ├── REST API/
│   ├── Socket/
│   └── gRPC/          ← {service}_service.proto
├── Интеграции со сторонними сервисами/
├── Работа с Rabbit MQ/
├── Работа с Redis/
└── Алгоритмы и методы бекенда/
```

gRPC-only services omit REST/Socket/Redis sections not used by the service.

# Specialized Rules (по подпапке)

| Подпапка | Rule file |
| :--- | :--- |
| `Методы API/DTO/` | `steos-docs-folder-04-dto.mdc` |
| `Методы API/gRPC/` + `.proto` | `steos-docs-folder-04-grpc.mdc` |
| `Методы API/REST API/` | `steos-docs-folder-04-rest-api.mdc` |
| `Методы API/Socket/` | `steos-docs-folder-04-socket.mdc` |
| `Интеграции со сторонними сервисами/` | `steos-docs-folder-04-integrations.mdc` |
| `Работа с Rabbit MQ/` | `steos-docs-folder-04-rabbitmq.mdc` |
| `Работа с Redis/` | `steos-docs-folder-04-redis.mdc` |
| `Алгоритмы и методы бекенда/` | `steos-docs-folder-04-algorithms.mdc` |

# Contract Layers

| Layer | Owner | Rule |
| :--- | :--- | :--- |
| **gRPC** | Microservice | Source of truth for RPC names, messages, errors |
| **REST / WebSocket** | API Gateway (BFF) | Map routes/events → gRPC; thin controllers |
| **DTO** | Gateway payload | JSON shapes; link to proto fields and `03` entities |

Every REST route and WebSocket event must cite the gRPC method(s) it delegates to (`#grpc-*`).

# Group Alignment

File names and group order in `Методы API/*`, Rabbit, Redis, Algorithms follow `01 — Функциональная спецификация` capability groups. Each table row includes SR code from `01`. Do not invent group names.

# Writing 04 at Scale

- `npx openskills read steos-docs-04-coordinator`
- `@docs-04-coordinator` → `@docs-04-writer` → `@docs-04-verifier`

Verifier writes ISSUEs to `99 - Staging`; do not delete staging.

# Consistency Checks

Before marking `04` complete:

- SR codes in gRPC/REST/Socket/Rabbit tables exist in `01`.
- Entity/DTO/proto fields trace to `03`.
- Every REST endpoint documents underlying gRPC with link to `#grpc-MethodName`.
- Every WebSocket event documents triggering gRPC method(s).
- proto `rpc` names match gRPC markdown and anchors `#grpc-*`.
- RabbitMQ/Redis key patterns and flows match `02` КАР decisions.
- Mismatch → ISSUE in `99 - Staging`.
