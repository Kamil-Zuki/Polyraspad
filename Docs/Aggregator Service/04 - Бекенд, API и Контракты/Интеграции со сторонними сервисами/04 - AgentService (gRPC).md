# Введение

In-app AI assistant threads. Proto: `agent.proto`. Config: `AggregatorService:AgentServiceBaseUrl`.

# Общая информация

| Параметр | Значение |
| :--- | :--- |
| **SR** | SR-AGG-AGENT-* |
| **Persistence** | AgentService EF Core (не на BFF) |

# gRPC методы

| REST | gRPC |
| :--- | :--- |
| CRUD /api/agent/threads | CreateThread, GetThread, ListThreads, DeleteThread, ArchiveThread |
| /messages | ListMessages, CreateMessage |
| /runs | CreateRun, GetRun |

# Cross-service

AgentService может вызывать VocabularyService для tool actions (mining) — внутренняя топология, не через Aggregator.
