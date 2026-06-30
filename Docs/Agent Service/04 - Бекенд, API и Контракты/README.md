# 04 — Бекенд, API и Контракты

Статус заполнения folder `04` для **Agent Service** (2026-06-28).

| Подпапка | Статус | Файлы |
| :--- | :--- | :--- |
| **Методы API / gRPC** | ✅ Complete | `agent.proto`, `00` overview, groups `01`–`03` (9 RPC, anchors `#grpc-*`) |
| **Методы API / DTO** | ✅ Complete | `00` overview, groups `01`–`03` (anchors `#dto-*`) |
| **Интеграции** | ✅ Complete | Vocabulary gRPC, LLM HTTP |
| **Алгоритмы** | ✅ Complete | `00`–`07` (threads, ExecuteRun, domain/intent, learning, nav, LLM, ops) |
| **REST API** | ➖ N/A | Public REST на Aggregator Service |
| **WebSocket** | ➖ N/A | — |
| **Redis / RabbitMQ** | ➖ N/A | — |

## Навигация

| Подпапка | Назначение |
| :--- | :--- |
| [[Методы API/gRPC/00 - gRPC - Общая информация]] | gRPC service `pvs.agent.grpc.AgentService`, 4 группы |
| [[Методы API/gRPC/agent.proto]] | Копия контракта (`AgentService/Protos/agent.proto`) |
| [[Методы API/DTO/00 - DTO - Общая информация]] | Proto messages / mapping |
| [[Интеграции со сторонними сервисами/00 - Интеграции - Общая информация]] | Vocabulary gRPC + LLM HTTP |
| [[Алгоритмы и методы бекенда/00 - Алгоритмы - Общая информация]] | Orchestrator, router, policy, tools |

**Proto source of truth (code):** `AgentService/Protos/agent.proto`.

**Public REST mapping:** `Docs/Aggregator Service/04/.../REST` Agent routes → gRPC methods in [[Методы API/gRPC/00 - gRPC - Общая информация]].
