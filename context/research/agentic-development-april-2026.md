# Agentic Development Notes - April 2026

## Sources

- OpenAI Codex repository docs reference `AGENTS.md` and describe hierarchical agent guidance behavior: https://github.com/openai/codex/blob/main/docs/agents_md.md
- The AGENTS.md project describes the file as a predictable, README-like place for coding-agent context and instructions: https://github.com/agentsmd/agents.md

## Local Interpretation

For Polyraspad, we use:

- root `AGENTS.md` as the repository entry point;
- `context/agents/AGENTS.md` as the deeper operating model;
- `context/rules/` for stable project constraints;
- `context/skills/` for task playbooks;
- `context/plans/` for active and historical implementation plans;
- `Docs/` for official human-facing documentation.

## Design Choice

We keep agent operational memory separate from official docs:

- `context/` changes often and helps agents execute;
- `Docs/` changes when the project source of truth changes.
