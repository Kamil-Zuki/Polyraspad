# AGENTS.md

This file is the repository-level operating guide for AI coding agents.

## Repository Map

- `Docs/` is the authoritative project documentation for humans and long-lived specs.
- `context/` is the operational memory for agents: active plans, rules, skills, prompts, research notes, and working decisions.
- `polyraspad-frontend/` contains the Next.js frontend.
- `AggregatorService/`, `VocabularyService/`, `MediaService/`, and `authorization-module/` contain backend services.
- `*.Tests/` projects contain backend tests.

## Required First Reads

For non-trivial work, read these before editing:

1. `context/README.md`
2. `context/agents/AGENTS.md`
3. relevant files in `context/rules/`
4. relevant active plan in `context/plans/active/`

## Documentation Boundary

- Put official, stable docs in `Docs/`.
- Put active implementation plans, agent instructions, research notes, and reusable agent skills in `context/`.
- When a plan becomes stable architecture, summarize the decision in `context/decisions/` and move final user-facing docs into `Docs/`.

## Work Rules

- Prefer existing project patterns over new abstractions.
- Keep changes scoped to the user request.
- Do not revert unrelated user changes.
- Use `rg` for searching.
- Use `apply_patch` for manual edits.
- Verify meaningful changes with the narrowest useful tests or build command.

## Current Product Direction

The active learning direction is the LingQ-style reader model:

- learn through real word forms and phrases;
- do not use lemmas as the basis for knowledge status, duplicate checks, statistics, or card creation;
- reader becomes the primary learning surface.

See `context/plans/active/lingq-reader-implementation-plan.md`.
