# Product Agent Task

Plan ID: `agent-persistence-phase4`
Agent: `product-agent`
Status: done
Can run in parallel: yes (with backend schema draft, before REST contract freeze)

## Locked Decisions

| # | Decision |
|---|----------|
| MVP thread UI | One active thread per project — no thread list UI in Phase 4 |
| Clear chat | Archive now via `POST /threads/{id}/archive`; new thread on next send only |
| Offline/API failure | localStorage cache fallback + non-blocking sync warning banners |
| Thread title | First user prompt, normalized, max 60 chars + ellipsis; fallback "New conversation" |
| Orchestration | Client-side `executeAgentTool()` unchanged; backend stores run bundle only |

## Locked Acceptance Criteria

### Thread model (MVP)

**Given** an authenticated user on `/dashboard` with a selected project  
**When** the page loads  
**Then** the UI loads the **latest non-archived thread** for `(userId, projectId)`  
**And** no thread list / history picker is shown in Phase 4

**Given** no active thread exists for the project  
**When** the user opens `/dashboard`  
**Then** the empty state shows suggested-prompt copy  
**And** no thread is created until the user sends a message

**Given** the user switches projects  
**When** the new project context loads  
**Then** messages for the previous project are not visible  
**And** the latest active thread for the new project loads (or empty state)

### Send message & persistence

**Given** an active thread with messages  
**When** the user sends a prompt  
**Then** user message appears optimistically; frontend runs domain gate + `executeAgentTool()`  
**And** `POST /threads/{threadId}/runs` persists user msg, assistant msg, domain decision, tool calls atomically

**Given** no active thread (first send or post-clear)  
**When** the user sends a message  
**Then** a new thread is created and title set from first user prompt (server-side, max 60 chars)

**Given** an out-of-scope prompt  
**When** the user sends it  
**Then** refusal is shown; `AgentDomainDecision` persisted with `allowed: false`, `category: out_of_scope`

### Clear chat

**Given** a thread with messages  
**When** the user clicks Clear chat  
**Then** UI clears; `POST /threads/{threadId}/archive`; no new thread until next send

### Cross-device / offline

**Given** messages sent on device A  
**When** user opens dashboard on device B  
**Then** same thread messages load from backend in `CreatedAt` order

**Given** backend unreachable on load  
**Then** show cached localStorage + banner: "Couldn't sync chat history. Showing messages saved on this device."

**Given** persist fails after send  
**Then** keep UI messages + banner: "Your message wasn't saved to the server yet. We'll retry when you're back online."

### Security

Cross-user or wrong-project thread access → **404** (not 403).

### Locked copy

| State | Copy |
|-------|------|
| Load fallback | Couldn't sync chat history. Showing messages saved on this device. |
| Persist failure | Your message wasn't saved to the server yet. We'll retry when you're back online. |
| Default thread title | New conversation |

## REST nuances for backend

1. `GET /threads?projectId=` — non-archived only, `UpdatedAt DESC`; frontend takes `[0]`
2. `POST /threads/{id}/runs` — atomic: messages + run + tool calls + domain decision
3. Lazy thread create on first run if no threadId (document in API)
4. Title derived server-side from first user message
5. Metadata cap ~32 KB per message
