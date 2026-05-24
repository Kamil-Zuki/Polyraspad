# Frontend Task

Plan ID: `language-agent-boundary`
Agent: `frontend-agent`
Status: done
Can run in parallel: no (after product)

## Files changed
- `polyraspad-frontend/src/lib/agent/agent-domain-policy.ts` — classifyAgentDomain, refusal copy, suggested prompts
- `polyraspad-frontend/src/lib/agent/agent-intent-router.ts` — out_of_scope routing, default-deny unknown intents
- `polyraspad-frontend/src/lib/agent/agent-tool-registry.ts` — handleOutOfScope, constrained general_answer, LLM domain gate in executeAgentTool
- `polyraspad-frontend/src/lib/agent/agent-message.ts` — intentCategory, refusal, suggestedPrompts
- `polyraspad-frontend/src/components/dashboard/agent-chat/agent-chat-thread.tsx` — refusal bubble + suggestion chips
- `polyraspad-frontend/src/lib/agent/agent-domain-policy.test.ts` — domain + router tests
- `polyraspad-frontend/src/lib/agent/agent-tool-registry.test.ts` — refusal + bypass regression
- `context/decisions/agent-persistence-model.md`, `agent-service-boundary.md`, `agent-home-dashboard-followup.md`

## Defense-in-depth
`executeAgentTool` re-checks `classifyAgentDomain` before any LLM-backed tool so explain/grammar routes cannot bypass the gate (e.g. "Explain how to implement binary search in Python").

## Verification
- `npm test -- --run src/lib/agent/agent-domain-policy.test.ts src/lib/agent/agent-tool-registry.test.ts src/components/dashboard/agent-chat/agent-chat-thread.test.tsx src/app/dashboard/page.test.tsx src/app/dashboard/page.empty.test.tsx src/components/dashboard/agent-chat/agent-chat-shell.test.tsx` — 24 passed

## Out of scope (deferred)
- `polyguide-agent.ts` editor prompt hardening (Phase 2 partial)
- Backend `/api/agent/threads` and AgentService microservice
