# Cursor Lead Plans

`.cursor/plans/` stores coordination plans created by `lead-agent`.

## Lifecycle (backlog → active → archive)

1. **Backlog** — черновики и отложенные планы: `.cursor/plans/backlog/<plan-file>.plan.md`. Здесь же может жить связанная папка задач `.cursor/tasks/backlog/<plan-id>/`.
2. **Active** — план в работе: перенести файл в `.cursor/plans/active/<plan-file>.plan.md`; папку задач — в `.cursor/tasks/active/<plan-id>/` (тот же `plan-id`, что поле `name` во frontmatter).
3. **Archive** — после закрытия плана: `.cursor/plans/archive/<plan-file>.plan.md` и `.cursor/tasks/archive/<plan-id>/` (см. Archive Rule ниже).

Планы и задачи не появляются в `archive/` напрямую из backlog без прохода через `active/`, если только lead-agent явно не решит заархивировать отменённый черновик (редкий случай).

## Where Plans Live

- Backlog (черновики): `.cursor/plans/backlog/<plan-file>.plan.md`
- Active plans: `.cursor/plans/active/<plan-file>.plan.md`
- Completed plans: переносить в `.cursor/plans/archive/<plan-file>.plan.md` (не удалять).
- Stable architecture or product decisions must be promoted to `context/decisions/`, `context/plans/`, or `Docs/` before or when closing the plan (архив не заменяет авторитетные документы).

Не класть рабочие `.plan.md` в корень `.cursor/plans/` — только `backlog/`, `active/` или `archive/`.

## Имя файла (обязательно)

Расширение: **`.plan.md`** (не `.md`).

Шаблон имени:

```text
<name>_<hash>.plan.md
```

| Часть | Правило | Пример |
|-------|---------|--------|
| `<name>` | kebab-case, совпадает с полем `name:` во frontmatter | `study-good-repeat-fix` |
| `<hash>` | опциональный короткий суффикс Cursor (обычно 8 hex); при создании в IDE может появиться автоматически | `1aec425e` |
| `plan-id` для папок задач | **только** `<name>` без `_<hash>` | `study-good-repeat-fix` |

Примеры:

```text
.cursor/plans/active/study-good-repeat-fix_1aec425e.plan.md   # файл плана
.cursor/tasks/active/study-good-repeat-fix/backend.md         # plan-id = name
```

Если hash ещё нет (ручное создание), допустимо `<name>.plan.md`; при появлении hash в IDE переименовать файл, **не** меняя `name` и `plan-id`.

## YAML frontmatter (обязательно)

Каждый `.plan.md` начинается с YAML frontmatter между `---`. Поле `todos` — **структурированный список**, не markdown-чеклист в frontmatter.

```yaml
---
name: study-good-repeat-fix
overview: Fix Study queue migration so reviewed due cards are not reintroduced from stale legacy Redis queues, which makes Good appear stuck at 1 day.
todos:
  - id: fix-legacy-queue-resurrection
    content: Update AnkiStudyQueueService so legacy Redis queues are deleted or migrated once, never reused repeatedly.
    status: pending
  - id: add-study-queue-regression
    content: Add a focused regression test proving stale legacy queues cannot resurrect a reviewed due card.
    status: pending
  - id: verify-study-good-flow
    content: Run backend tests and verify Good no longer repeats the same 1d card.
    status: pending
isProject: false
---
```

### Поля frontmatter

| Поле | Обязательно | Описание |
|------|-------------|----------|
| `name` | да | kebab-case ID плана; = `plan-id` для `.cursor/tasks/<stage>/<plan-id>/` |
| `overview` | да | 1–2 предложения: цель и ожидаемый результат |
| `todos` | да | Массив шагов (см. ниже) |
| `isProject` | да | Для lead-coordination планов: `false` |

### Формат `todos`

Каждый элемент:

```yaml
- id: <kebab-case-slug>      # уникален в пределах плана
  content: <что сделать>     # одно конкретное действие
  status: <status>           # см. допустимые значения
```

Допустимые `status`:

| status | Когда |
|--------|--------|
| `pending` | ещё не начато |
| `in_progress` | в работе |
| `completed` | сделано и проверено |
| `cancelled` | снято из scope |

При закрытии плана все релевантные todo должны быть `completed` или `cancelled`.

Обновляй `status` в frontmatter по ходу работы — это источник истины для Cursor и lead-agent.

## Тело плана (после frontmatter)

Ниже `---` — обычный Markdown: Goal, Files, Implementation, Verification, Agents, Tasks и т.д. Секция `## Todos` в теле **не обязательна**; если есть, дублирует frontmatter для чтения людьми, но при расхождении верь frontmatter.

## Plan Template (полный пример)

```markdown
---
name: study-good-repeat-fix
overview: Fix Study queue migration so reviewed due cards are not reintroduced from stale legacy Redis queues, which makes Good appear stuck at 1 day.
todos:
  - id: fix-legacy-queue-resurrection
    content: Update AnkiStudyQueueService so legacy Redis queues are deleted or migrated once, never reused repeatedly.
    status: completed
  - id: add-study-queue-regression
    content: Add a focused regression test proving stale legacy queues cannot resurrect a reviewed due card.
    status: completed
  - id: verify-study-good-flow
    content: Run backend tests and verify Good no longer repeats the same 1d card.
    status: completed
isProject: false
---

# Fix Study Good Repeats 1 Day

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
- [ ] Все frontmatter `todos` — `completed` или `cancelled`
- [ ] Task-папка перенесена в archive (см. `.cursor/tasks/README.md`)
- [ ] План перенесён в `.cursor/plans/archive/<plan-file>.plan.md`
- [ ] Durable decisions promoted if needed
```

## Start work (из backlog в active)

Когда план берёт в работу:

1. Перенести `.cursor/plans/backlog/<plan-file>.plan.md` → `.cursor/plans/active/<plan-file>.plan.md`.
2. Обновить todo `status` в frontmatter (`in_progress` / `completed` по мере выполнения).
3. Если есть `.cursor/tasks/backlog/<plan-id>/`, перенести целиком в `.cursor/tasks/active/<plan-id>/`.

## Archive Rule

When all task files for a plan are complete (или план закрыт по решению lead-agent):

1. Verify the feature or plan outcome.
2. Promote durable decisions to `context/` or `Docs/` if needed.
3. Убедиться, что все `todos` во frontmatter — `completed` или `cancelled`.
4. Перенести `.cursor/tasks/active/<plan-id>/` → `.cursor/tasks/archive/<plan-id>/` (целиком, чтобы сохранить историю task-ов).
5. Перенести `.cursor/plans/active/<plan-file>.plan.md` → `.cursor/plans/archive/<plan-file>.plan.md`.
