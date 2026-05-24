# Reviewer Agent Task

Plan ID: `agent-persistence-phase4`
Agent: `reviewer-agent`
Status: done
Can run in parallel: no (after backend + frontend slices)

## Objective

Review Phase 4 agent persistence for security, contract consistency, regression risks, and LingQ domain guardrails on stored agent data.

## Inputs

- Plan: `.cursor/plans/backlog/agent-persistence-phase4.plan.md`
- Backend handoff: AgentController, AgentService, migration, tests
- Frontend handoff: use-agent-chat migration, agent client
- Decisions: `context/decisions/agent-persistence-model.md`, `context/decisions/agent-service-boundary.md`

## Scope

- Auth scoping: all reads/writes filtered by authenticated user
- Project ownership validation before thread create/read
- No cross-user thread leakage (404 vs 403 policy)
- REST ↔ gRPC ↔ frontend type alignment
- Migration safety (new tables only)
- Domain decisions persisted on every run including refusals
- Term-first guardrails unchanged in tool payloads

## Out of Scope

- Re-architecting to AgentService
- Product UX copy review (unless security-related)

## Deliverables

- Review findings: blockers / should-fix / nice-to-have
- Confirmation or gaps on test coverage

## Verification

- Re-run cited test filters from backend and frontend handoffs

## Handoff

- merge-ready yes/no
- required fixes before archive
- residual risks
