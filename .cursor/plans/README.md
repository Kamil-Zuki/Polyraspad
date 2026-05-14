# Cursor Lead Plans

`.cursor/plans/` stores coordination plans created by `lead-agent`.

## Lifecycle (backlog → active → archive)

1. **Backlog** — черновики и отложенные планы: `.cursor/plans/backlog/<plan-id>.md` (`Status: backlog`). Здесь же может жить связанная папка задач `.cursor/tasks/backlog/<plan-id>/`.
2. **Active** — план в работе: перенести файл в `.cursor/plans/active/<plan-id>.md`, обновить `Status: active`; папку задач — в `.cursor/tasks/active/<plan-id>/` (те же `plan-id`, чтобы не терять ссылки).
3. **Archive** — после закрытия плана: `.cursor/plans/archive/<plan-id>.md` и `.cursor/tasks/archive/<plan-id>/` (см. Archive Rule ниже).

Планы и задачи не появляются в `archive/` напрямую из backlog без прохода через `active/`, если только lead-agent явно не решит заархивировать отменённый черновик (редкий случай).

## Where Plans Live

- Backlog (черновики): `.cursor/plans/backlog/<plan-id>.md`
- Active plans: `.cursor/plans/active/<plan-id>.md`
- Completed plans: переносить в `.cursor/plans/archive/<plan-id>.md` (не удалять).
- Stable architecture or product decisions must be promoted to `context/decisions/`, `context/plans/`, or `Docs/` before or when closing the plan (архив не заменяет авторитетные документы).

## Plan ID

Use a short kebab-case ID:

```text
reader-phrase-lingq-2026-05-13
```

## Plan Template

```markdown
# <Plan Title>

Plan ID: `<plan-id>`
Status: backlog | active
Created: YYYY-MM-DD
Owner: `lead-agent`

## Goal
<What should change for the user or system>

## Out of Scope
- <What is explicitly not part of this plan>

## Agents
- `product-agent`: <responsibility or "not needed">
- `backend-agent`: <responsibility or "not needed">
- `frontend-agent`: <responsibility or "not needed">
- `reviewer-agent`: <responsibility or "not needed">

## Contracts To Lock
- <REST/gRPC DTO/API client/UI state/data assumption>

## Tasks
- Backlog: `.cursor/tasks/backlog/<plan-id>/…` (пока план в backlog)
- Active: `.cursor/tasks/active/<plan-id>/product.md`
- `.cursor/tasks/active/<plan-id>/backend.md`
- `.cursor/tasks/active/<plan-id>/frontend.md`
- `.cursor/tasks/active/<plan-id>/review.md`

## Verification
- <test/build/check>

## Cleanup
- [ ] (если план начинался в backlog) Перенос `backlog/` → `active/` выполнен до или в ходе работы
- [ ] Task-папка перенесена в archive (см. `.cursor/tasks/README.md`)
- [ ] План перенесён в `.cursor/plans/archive/<plan-id>.md`, в шапке при желании `Status: archived`
- [ ] Durable decisions promoted if needed
```

## Start work (из backlog в active)

Когда план берёт в работу:

1. Перенести `.cursor/plans/backlog/<plan-id>.md` → `.cursor/plans/active/<plan-id>.md`, выставить `Status: active`.
2. Если есть `.cursor/tasks/backlog/<plan-id>/`, перенести целиком в `.cursor/tasks/active/<plan-id>/`.

## Archive Rule

When all task files for a plan are complete (или план закрыт по решению lead-agent):

1. Verify the feature or plan outcome.
2. Promote durable decisions to `context/` or `Docs/` if needed.
3. Перенести `.cursor/tasks/active/<plan-id>/` → `.cursor/tasks/archive/<plan-id>/` (целиком, чтобы сохранить историю task-ов).
4. Перенести `.cursor/plans/active/<plan-id>.md` → `.cursor/plans/archive/<plan-id>.md`.
