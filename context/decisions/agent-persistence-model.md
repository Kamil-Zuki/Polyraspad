# Agent Persistence Model

Decision date: 2026-05-24

## Why backend persistence is needed

Current MVP stores the last 40 messages per project in browser `localStorage` via [`use-agent-chat.ts`](../../polyraspad-frontend/src/lib/agent/use-agent-chat.ts). That is enough for prototyping but not for real agents:

- no cross-device sync;
- no audit trail for tool calls or refusals;
- no server-side evaluation;
- no resumable runs;
- no analytics on agent quality.

## Phase 1: Aggregator-backed threads (before AgentService)

Add REST endpoints under Aggregator first:

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/agent/threads?projectId=` | List threads for user/project |
| POST | `/api/agent/threads` | Create thread |
| GET | `/api/agent/threads/{threadId}/messages` | Load message history |
| POST | `/api/agent/threads/{threadId}/messages` | Append user message |
| POST | `/api/agent/threads/{threadId}/runs` | Execute agent run and persist assistant output |

Frontend keeps `localStorage` only as offline cache until backend is stable.

## Core tables

### AgentThreads

- `Id` (uuid)
- `UserId`
- `ProjectId`
- `Title` (nullable, derived from first prompt)
- `CreatedAt`
- `UpdatedAt`
- `ArchivedAt` (nullable)

### AgentMessages

- `Id` (uuid)
- `ThreadId`
- `Role` (`user`, `assistant`, `system`, `tool`)
- `Content`
- `MetadataJson` (actions, refusal, suggested prompts)
- `CreatedAt`

### AgentRuns

- `Id` (uuid)
- `ThreadId`
- `Status` (`running`, `completed`, `failed`, `cancelled`)
- `Model`
- `StartedAt`
- `CompletedAt`
- `Error` (nullable)

### AgentToolCalls

- `Id` (uuid)
- `RunId`
- `ToolName`
- `InputJson`
- `OutputJson`
- `Status`
- `CreatedAt`

### AgentDomainDecisions

- `Id` (uuid)
- `RunId`
- `Allowed` (bool)
- `Category` (`language_learning`, `product_navigation`, `progress`, `out_of_scope`)
- `Reason` (nullable)
- `UserTextHash` or truncated preview for audit
- `CreatedAt`

### AgentArtifacts

- `Id` (uuid)
- `RunId`
- `Kind` (`editor_draft`, `import_draft`, `term_list`, `study_plan`)
- `PayloadJson`
- `CreatedAt`

## Indexing

- `(UserId, ProjectId, UpdatedAt DESC)` on `AgentThreads`
- `(ThreadId, CreatedAt)` on `AgentMessages`
- `(RunId, CreatedAt)` on `AgentToolCalls`
- `(RunId)` on `AgentDomainDecisions`

## Security

- All queries scoped by authenticated `UserId`.
- `ProjectId` must belong to the user before thread creation or reads.
- Never expose another user's thread/messages.
- Store domain decisions for every run to audit out-of-scope refusals.

## Frontend migration

1. On dashboard load: fetch latest thread from backend.
2. On send: POST message + run; append assistant response from server.
3. Mirror to `localStorage` only as cache.
4. On clear chat: archive thread server-side or create a new thread.

## Later tables

- `AgentMemories` — durable learner preferences and facts
- `AgentPreferences` — model/tool settings per user/project
- `AgentEvaluationEvents` — thumbs up/down, report, quality labels
- `AgentEmbeddings` — optional semantic memory via Postgres + `pgvector`
