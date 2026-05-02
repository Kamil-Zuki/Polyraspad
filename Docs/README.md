# Docs

`Docs/` is the official project documentation area.

Use this folder for stable documents that should remain useful outside a single implementation session:

- architecture overviews;
- API documentation;
- data model documentation;
- deployment and runbook documentation;
- product specs;
- design references;
- user-facing or team-facing explanations.

Use `context/` for agent working memory, active plans, skills, rules, prompts, and research notes.

## Relationship With `context/`

- `Docs/` answers: "What is true about the project?"
- `context/` answers: "How should agents work on the project right now?"

When an active plan is completed, extract the stable result into `Docs/` and keep the historical implementation plan in `context/plans/completed/`.
