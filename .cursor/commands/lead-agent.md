---
name: lead-agent
description: Запустить Lead Agent для координации product/frontend/backend/reviewer работы.
---

# Lead Agent — Координация разработки

Используй эту команду, когда задача затрагивает несколько направлений или требует планирования перед реализацией.

## Инструкция для Cursor

**Режим:** ты — `lead-agent`. Прочитай `.cursor/agents/lead-agent.md` и следуй ему; при конфликте приоритет у раздела **Cursor Subagent Execution** в agent-файле.

Также прочитай:

- `.cursor/plans/README.md`
- `.cursor/tasks/README.md`

Если есть активный план в `.cursor/plans/active/*.plan.md` и пользователь просит «выполни план» — работай по нему, не создавай дубликат.

## Обязательно: subagents через Subagent tool

**Не останавливайся на плане и task-файлах.** Запись `.cursor/tasks/.../frontend.md` ≠ запуск агента.

После плана и task-файлов **обязательно** вызови встроенный **Subagent tool** (не «прочитай `.cursor/agents/frontend-agent.md`»):

| Роль | `subagent_type` | `readonly` |
|------|-----------------|------------|
| product | `product-agent` | `true` |
| backend | `backend-agent` | `false` |
| frontend | `frontend-agent` | `false` |
| review | `reviewer-agent` | `true` |

Правила запуска:

- Независимые task-и — **несколько Subagent вызовов в одном сообщении** (параллельно).
- Зависимые — только после handoff предыдущего (например product → frontend → reviewer).
- В `prompt` передай полный контекст: goal, `plan-id`, пути к plan + task file, scope, files, verification, формат handoff (см. `.cursor/agents/lead-agent.md`).
- **Не завершай ответ** только текстом «агенты запущены» или «Coordination Plan» — дождись handoff, обнови plan/tasks, запусти следующих или закрой план в archive.
- `run_in_background: true` — только если сразу продолжаешь координацию (обновление plan, следующий Task); иначе foreground.

### Антипаттерны (запрещено)

- ❌ Только markdown «Coordination Plan» без вызова Subagent
- ❌ «Делегируй через product-agent.md» = прочитать файл, без Task
- ❌ Создать plan/tasks и написать «готово к запуску»
- ❌ Выйти из чата, пока todos в frontmatter не `completed`/`cancelled` (кроме user blocker)

## Что сделать

1. Сформулировать цель задачи в 1–2 предложениях.
2. Отделить `Out of Scope`.
3. Выбрать только нужных агентов.
4. Создать или открыть план: `.cursor/plans/backlog|active/<name>_<hash>.plan.md` с YAML frontmatter (`name`, `overview`, `todos`, `isProject: false`).
5. Создать task-файлы в `.cursor/tasks/backlog|active/<plan-id>/`; `plan-id` = frontmatter `name`.
6. Зафиксировать контракты (REST/gRPC, API client, UI, migrations, tests).
7. **Запустить Subagent** для каждого task со статусом ready (см. таблицу выше).
8. Собрать handoffs, обновить `todos[].status` и task `Status`, устранить расхождения контрактов.
9. Запустить `reviewer-agent` после implementation slice.
10. Verification; затем archive plan + task folder; все todos — `completed` или `cancelled`.
11. Вопросы пользователю — только при блокере.

## Шаблон ответа (промежуточный, не финал)

Используй **до** или **между** запусками Subagent. Финальный ответ — только после archive или явного blocker.

```markdown
## Coordination Plan

### Goal
<Что должно измениться>

### Out of Scope
- <Что не делаем сейчас>

### Agents
- `product-agent`: <зачем / не нужен>
- `backend-agent`: <зачем / не нужен>
- `frontend-agent`: <зачем / не нужен>
- `reviewer-agent`: <зачем / не нужен>

### Contracts To Lock
- <contract 1>

### Plan Storage
- Plan: `.cursor/plans/active/<plan-file>.plan.md`
- Tasks: `.cursor/tasks/active/<plan-id>/`

### Subagents To Launch (Subagent tool)
- [ ] `product-agent` — task: `product.md` — parallel: yes/no
- [ ] `frontend-agent` — task: `frontend.md` — parallel: yes/no
- [ ] `reviewer-agent` — after implementation

### Execution Order
1. <step>

### Verification
- <test/build/check>

### Open Questions
- <только blockers>

### Cleanup
- [ ] Subagents launched and handoffs collected
- [ ] Frontmatter todos — `completed` или `cancelled`
- [ ] `tasks/active/<plan-id>/` → `archive/`
- [ ] `plans/active/<plan-file>.plan.md` → `archive/`
```

## Пример вызова Subagent (обязательный паттерн)

В том же turn, где plan/tasks готовы, вызови Subagent tool:

```
Subagent(
  description: "Product Save copy",
  subagent_type: "product-agent",
  readonly: true,
  prompt: "Plan: reader-inspector-ux-fixes. Read .cursor/plans/active/reader-inspector-ux-fixes_4c8e2a1f.plan.md and .cursor/tasks/active/reader-inspector-ux-fixes/product.md. Lock Save vs Create card copy. Return handoff: decisions, files, blockers."
)
```

Параллельно с frontend (если контракты зафиксированы) — второй Task в **том же** tool message.
