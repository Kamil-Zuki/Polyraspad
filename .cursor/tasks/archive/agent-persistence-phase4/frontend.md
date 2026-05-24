# Frontend Agent Task

Plan ID: `agent-persistence-phase4`
Agent: `frontend-agent`
Status: done
Can run in parallel: no (depends on stable REST contract from backend)

## Objective

Migrate PolyGuide dashboard chat from `localStorage`-primary to backend-primary persistence using new agent API client and React Query hooks.

## Inputs

- Plan: `.cursor/plans/backlog/agent-persistence-phase4.plan.md`
- Current hook: `polyraspad-frontend/src/lib/agent/use-agent-chat.ts`
- Message model: `polyraspad-frontend/src/lib/agent/agent-message.ts`
- Tool registry: `polyraspad-frontend/src/lib/agent/agent-tool-registry.ts`
- Domain policy: `polyraspad-frontend/src/lib/agent/agent-domain-policy.ts`

## Scope

- Add `API_ENDPOINTS.AGENT` in `constants.ts`
- Add `agent-client.ts` + types in `lib/api/types.ts`
- React Query hooks: load thread/messages, create thread, post run bundle, archive thread
- Refactor `use-agent-chat.ts`:
  - backend load on project change
  - send: local `executeAgentTool` → POST run with tool calls + domain decision
  - clear: archive server thread
  - `localStorage` cache fallback + sync warning on failure
- Wire dashboard components if props change (minimal)

## Out of Scope

- Server-side orchestration
- Streaming UI
- Editor agent chat (unless trivial reuse; dashboard first)
- AgentService

## Deliverables

- Agent API client + hooks
- Migrated `use-agent-chat.ts`
- Tests: load/send/clear, cache fallback, metadata round-trip (refusal, suggestedPrompts, actions)

## Verification

```bash
cd polyraspad-frontend && npm test -- --run src/lib/agent
```

Manual: refresh `/dashboard` after send — messages restored from backend.

## Handoff

- files changed
- cache strategy summary
- any API contract mismatches found
- blockers
