# Cursor Lead Tasks

`.cursor/tasks/` stores task files created by `lead-agent` for specialist agents.

## Where Tasks Live

- Active task folders: `.cursor/tasks/active/<plan-id>/`
- Task files: `.cursor/tasks/active/<plan-id>/<agent>.md`
- Archived task folders: `.cursor/tasks/archive/<plan-id>/` (после закрытия плана — см. Archive Rule)

## Parallel Execution Rule

`lead-agent` may run independent tasks in parallel when their contracts are locked and they do not write the same files.

Good parallel split:

- `product-agent` defines acceptance criteria.
- `backend-agent` implements a stable controller/API contract.
- `frontend-agent` implements UI after the API client contract is known.
- `reviewer-agent` runs after implementation slices are ready.

Do not run tasks in parallel when:

- two agents need to edit the same files;
- frontend depends on an unstable backend DTO;
- migrations or public contracts are still undecided;
- a user decision blocks safe implementation.

## Task Template

```markdown
# <Agent> Task

Plan ID: `<plan-id>`
Agent: `<agent-name>`
Status: pending | in_progress | blocked | done
Can run in parallel: yes | no

## Objective
<What this agent must accomplish>

## Inputs
- Plan: `.cursor/plans/active/<plan-id>.md`
- Files/contracts to read:
  - `<path>`

## Scope
- <Allowed work>

## Out of Scope
- <Forbidden work>

## Deliverables
- <Expected code/doc/test output>

## Verification
- <Command/check>

## Handoff
- <What the lead-agent needs after completion>
```

## Archive Rule

When a task is complete and its result is reflected in code/docs/tests:

1. Report completion to `lead-agent`.
2. Обновить статус в task-файле (`Status: done`); файл можно не удалять — при закрытии плана переносится вся папка плана.
3. Когда план закрыт: перенести `.cursor/tasks/active/<plan-id>/` целиком в `.cursor/tasks/archive/<plan-id>/` (вместе с оставшимися `.md`).
