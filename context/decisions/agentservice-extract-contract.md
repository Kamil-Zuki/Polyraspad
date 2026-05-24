# AgentService Extract — Locked Contract

Decision date: 2026-05-24

## Ownership

- **AgentService** owns agent DB, orchestration, LLM calls, tool audit, artifacts.
- **VocabularyService** owns terms/cards/study/analytics data only.
- **Aggregator** keeps public REST `/api/agent/*` unchanged for frontend.

## gRPC

- Package: `pvs.agent.grpc`
- Proto file: `AgentService/Protos/agent.proto`
- Port: `5131` (HTTP/2, container internal)
- User identity from gRPC metadata (set by Aggregator), never trusted from request body alone.

## Project validation

- No FK from `agent_threads.project_id` to VocabularyService `projects`.
- AgentService validates `(userId, projectId)` via VocabularyService `ContentService.GetProjectDetails`.

## REST (unchanged)

- `GET/POST /api/agent/threads`
- `GET /api/agent/threads/{id}/messages`
- `POST /api/agent/threads/{id}/runs` — **server executes run** from user text
- `POST /api/agent/threads/{id}/archive`

## Orchestration

- Server-side domain gate + intent routing in AgentService.
- Tools call VocabularyService (analytics, AI) via gRPC; no direct DB access to vocabulary tables.
- Mutations require artifacts/actions metadata; no silent term/card writes.

## Migration

- New DB: `agent_service`
- One-time SQL copy from `vocabulary_service.internal.agent_*` tables
- Do not drop interim tables in VocabularyService until explicit cleanup migration approved
