# ADR-0001: Separate `context` From `Docs`

## Context

The repository needs both stable documentation and operational memory for agentic development.

Mixing active plans, agent rules, prompts, and official docs makes future work harder to navigate.

## Decision

- `Docs/` is the official documentation area.
- `context/` is the agentic working-memory area.
- Root `AGENTS.md` is the repository-level agent entry point.

## Consequences

- Active plans live under `context/plans/active/`.
- Stable docs live under `Docs/`.
- Completed implementation plans can remain in `context/plans/completed/`.
- Finalized architecture should be promoted to `Docs/` and summarized in `context/decisions/`.
