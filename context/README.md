# Context

`context/` is the operational memory of the project for agentic development.

Use this folder for documents that help agents and developers execute work:

- active implementation plans;
- agent instructions;
- coding and testing rules;
- reusable skills;
- prompts;
- research notes;
- architecture decisions that are still close to implementation.

For stable documentation intended as the project source of truth, use `Docs/`.

## Folder Structure

- `agents/` - instructions for AI agents, roles, and workflows.
- `rules/` - project rules for code, tests, frontend, backend, Git, Docker, and documentation.
- `plans/active/` - plans currently being implemented.
- `plans/backlog/` - approved plans not yet started.
- `plans/completed/` - implemented plans kept for history.
- `plans/archived/` - stale or superseded plans.
- `skills/` - reusable task-specific playbooks for agents.
- `decisions/` - ADR-style decisions and implementation rationale.
- `research/` - notes from external products, docs, experiments, and discovery.
- `product/` - product language, flows, UX principles, and glossary.
- `api/` - service/API notes for agent use.
- `database/` - schema and migration notes.
- `prompts/` - reusable prompts for implementation, review, and testing.

## Promotion Rule

When a document becomes stable and user/team-facing:

1. Keep the implementation trace in `context/`.
2. Add or update the official version in `Docs/`.
3. Link the two documents instead of duplicating large sections.

## Current Active Plans

- `plans/active/lingq-reader-implementation-plan.md`
