# Введение

DTO Agent Service — **protobuf messages** из [[../gRPC/agent.proto]] и internal C# DTOs в `AgentService/Dtos/Agent/`. Gateway REST на Aggregator маппится на те же gRPC messages через AutoMapper.

Поля согласованы с сущностями folder `03` и RPC blocks folder `04/gRPC`.

# 1. Группы DTO

| Группа | Файл | gRPC RPC |
| :--- | :--- | :--- |
| **Треды и сообщения** | [[01 - Треды и сообщения]] | ListThreads, CreateThread, GetThread, ListMessages, ArchiveThread |
| **Запуски и оркестрация** | [[02 - Запуски и оркестрация]] | CreateRun, ExecuteRun |
| **Артефакты** | [[03 - Артефакты]] | CreateArtifact, ListArtifacts |

# 2. Треды и сообщения

| DTO | Назначение | Request/Response |
| :--- | :--- | :--- |
| `AgentThreadListItem` | List item | Response |
| `AgentThreadResponse` | Thread detail | Response |
| `AgentMessageItem` | Message row | Response |
| `ListAgentMessagesRequest` | Cursor pagination | Request |
| `ListAgentMessagesResponse` | Messages page | Response |

# 3. Запуски и оркестрация

| DTO | Назначение | Request/Response |
| :--- | :--- | :--- |
| `AgentMessageInput` | Message persist input | Request (nested) |
| `AgentDomainDecisionInput` | Domain audit | Request (nested) |
| `AgentToolCallInput` | Tool audit | Request (nested) |
| `CreateAgentRunRequest` | Persist run | Request |
| `ExecuteAgentRunRequest` | Orchestrate run | Request |
| `AgentRunItem` | Run summary | Response (nested) |
| `CreateAgentRunResponse` | Run + messages | Response |

# 4. Артефакты

| DTO | Назначение | Request/Response |
| :--- | :--- | :--- |
| `CreateAgentArtifactRequest` | Create artifact | Request |
| `AgentArtifactItem` | Artifact row | Response |
| `ListAgentArtifactsRequest` | List filter | Request |
| `ListAgentArtifactsResponse` | Artifact list | Response |

## Internal DTOs (service layer)

| DTO | Mapping |
| :--- | :--- |
| `CreateAgentRunDto` | Proto → `AgentThreadService.CreateRunAsync` |
| `ExecuteAgentRunDto` | ExecuteRun request slice |
| `AgentThreadListItemDto` | EF → gRPC list item |

AutoMapper profile: `AutoMappingProfile`.

## Valid enum-like values

**Domain categories:** `language_learning` | `product_navigation` | `progress` | `out_of_scope`

**Message roles:** `user` | `assistant` | `system` | `tool`

**Tool call status:** `completed` | `failed`

Детальные блоки с `#dto-*` anchors — в group files `01`–`03`.
