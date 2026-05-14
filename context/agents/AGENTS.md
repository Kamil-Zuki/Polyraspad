# Agent Operating Guide

This folder contains the agent-facing instructions for Polyraspad.

## Agent Development Model

This is the repository's agentic development model as of April 2026.

Use a plan-driven, evidence-first workflow:

1. Read the active plan or create one if the work spans multiple modules.
2. Inspect the code before proposing architecture.
3. Make the smallest useful implementation step.
4. Verify with focused tests or a build.
5. Update the plan when scope, risk, or implementation order changes.
6. Promote stable outcomes to `Docs/` only when they become durable documentation.

## Agent Roles

Cursor-executable subagents live in `.cursor/agents/`.

Temporary **lead-agent** coordination (multi-agent runs) uses `.cursor/plans/` and `.cursor/tasks/` with lifecycle `backlog` → `active` → `archive` — see `.cursor/plans/README.md` and `.cursor/tasks/README.md`. Product and implementation roadmaps stay in `context/plans/` (see `context/README.md`).

Longer context role guides live in `agents/roles/`:

- `lead-agent.md` - coordination across product, frontend, backend, testing, review, and docs.
- `product-agent.md` - product behavior, UX flows, and acceptance criteria.
- `frontend-agent.md` - Next.js UI, reader UX, state, and component work.
- `backend-agent.md` - .NET services, API contracts, data, migrations.
- `reviewer-agent.md` - code review, risk checks, tests, regressions.

These are role guides, not separate processes. Use the relevant role when the task matches it.

## Scope Rules

- Repository-wide instructions start at root `AGENTS.md`.
- `context/` keeps deeper operational guidance.
- If a future subdirectory needs stricter rules, add a local `AGENTS.md` in that directory.

## Mandatory Habits

- Search with `rg`.
- Preserve unrelated user changes.
- Prefer existing project patterns.
- Keep edits focused.
- Document new recurring rules in `context/rules/`.
- Document reusable workflows in `context/skills/` or `context/agents/workflows/`.

## LingQ Reader Direction

For Library, Reader, vocabulary status, and card creation:

- real forms and phrases are the learning units;
- lemmas are legacy/supporting metadata only;
- duplicate checks use exact normalized term or phrase;
- reader should be the primary learning surface.

See `context/plans/active/lingq-reader-implementation-plan.md`.
