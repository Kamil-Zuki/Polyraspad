# AGENTS.md

This file is the repository-level operating guide for AI coding agents. It stays at the **repository root** on purpose (tooling and hierarchical agent docs expect a root entry point). Deeper guidance lives in `context/`; Cursor-specific wiring lives in `.cursor/`.

## Repository Map

- `Docs/` — authoritative documentation for humans and long-lived specs.
- `context/` — operational memory: product/implementation plans, rules, skills, prompts, research, ADRs-in-progress.
- `.cursor/` — Cursor-native material: subagent definitions, commands, **always-applied rules** (`.cursor/rules/`), skills, and **lead coordination** plans/tasks.
- `polyraspad-frontend/` — Next.js frontend.
- `AggregatorService/`, `VocabularyService/`, `MediaService/`, `authorization-module/` — backend services.
- `*.Tests/` — backend test projects.

### `.cursor/` layout (high level)

- `agents/`, `commands/` — agent roles and checklists the IDE can run.
- `rules/` — glob-scoped rules (for example `01-repo-operating-model.mdc`).
- `skills/` — technology playbooks.
- `plans/` — **lead-agent** coordination plans: `backlog/` → `active/` → `archive/` (see `.cursor/plans/README.md`).
- `tasks/` — **lead-agent** task folders per plan id, same lifecycle (see `.cursor/tasks/README.md`).

### Two kinds of “plans” (do not confuse)

| Location | Purpose |
|----------|---------|
| `context/plans/` (`active/`, `backlog/`, `completed/`, … per `context/README.md`) | Product and implementation plans for the team; stable narrative of what we build. |
| `.cursor/plans/` + `.cursor/tasks/` | Short-lived coordination for multi-agent runs (`lead-agent`); moves **backlog → active → archive** when work finishes. |

## Required First Reads

For non-trivial work, read before editing:

1. `context/README.md` — what belongs in `context/` vs `Docs/`.
2. `context/agents/AGENTS.md` — agent model, roles, habits.
3. Relevant files under `context/rules/` for the area you touch.
4. Relevant **active** plan under `context/plans/active/` when the work follows that roadmap.
5. `.cursor/rules/01-repo-operating-model.mdc` — repo boundaries and `.cursor/` structure.
6. If coordinating via `lead-agent`: `.cursor/plans/README.md` and `.cursor/tasks/README.md`.

## Documentation Boundary

- Put official, stable docs in `Docs/`.
- Put active implementation plans, agent instructions, research, and working decisions in `context/`.
- Put Cursor-executable instructions (commands, agent frontmatter, auto rules, lead coordination files) in `.cursor/`.
- When a plan becomes stable architecture, summarize in `context/decisions/` and promote user-facing text to `Docs/` as needed.

## Work Rules

- Prefer existing project patterns over new abstractions.
- Keep changes scoped to the user request.
- Do not revert unrelated user changes.
- Search with `rg`.
- Make small, reviewable edits; verify with the narrowest useful test or build command.

## Current Product Direction

The active learning direction is the LingQ-style reader model:

- learn through real word forms and phrases;
- do not use lemmas as the basis for knowledge status, duplicate checks, statistics, or card creation;
- reader becomes the primary learning surface.

Implementation narrative: `context/plans/active/lingq-reader-implementation-plan.md`.  
Optional Cursor-side coordination roadmap (if present): `.cursor/plans/active/reader-library-lingq-roadmap-2026-05-13.md`.
