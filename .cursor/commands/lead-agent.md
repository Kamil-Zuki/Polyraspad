---
name: lead-agent
description: Запустить Lead Agent для координации product/frontend/backend/reviewer работы.
---

# Lead Agent — Координация разработки

Используй эту команду, когда задача затрагивает несколько направлений или требует планирования перед реализацией.

## Инструкция для Cursor

Перед началом прочитай `.cursor/agents/lead-agent.md` и действуй как `lead-agent`.
Также прочитай:

- `.cursor/plans/README.md`
- `.cursor/tasks/README.md`

Если задача требует специализированной работы, делегируй или маршрутизируй её через:

- `.cursor/agents/product-agent.md` — product behavior, acceptance criteria, терминология
- `.cursor/agents/backend-agent.md` — .NET backend, controllers, DTO, gRPC, migrations
- `.cursor/agents/frontend-agent.md` — Next.js, Reader UX, React Query, API clients
- `.cursor/agents/reviewer-agent.md` — review, regressions, tests, architecture risks

## Что сделать

1. Сформулировать цель задачи в 1-2 предложениях.
2. Отделить `Out of Scope`.
3. Выбрать только нужных агентов.
4. Создать план: черновик в `.cursor/plans/backlog/<plan-id>.md` (`Status: backlog`) или сразу рабочий `.cursor/plans/active/<plan-id>.md` (`Status: active`). При старте работы перенести backlog → active и обновить статус (см. `.cursor/plans/README.md`).
5. Создать task-файлы в `.cursor/tasks/backlog/<plan-id>/` или `.cursor/tasks/active/<plan-id>/` в той же стадии, что и план; при переходе плана в active перенести папку задач в `tasks/active/<plan-id>/`.
6. Зафиксировать контракты, которые нельзя рассинхронизировать:
   - REST/gRPC DTO
   - frontend API client
   - UI states
   - migrations/data assumptions
   - tests/verification gates
7. Запустить независимые task-и параллельно, если они не конфликтуют по файлам и не зависят от незакрытых контрактов.
8. Составить порядок выполнения для зависимых task-ов.
9. Назвать проверки, которые должны пройти перед завершением.
10. После выполнения: при закрытии плана перенести папку `.cursor/tasks/active/<plan-id>/` в `.cursor/tasks/archive/<plan-id>/` и файл плана из `plans/active/` в `plans/archive/` (не удалять реализованные планы).
11. Задать пользователю вопросы только если они блокируют безопасное продолжение.

## Шаблон ответа

```markdown
## Coordination Plan

### Goal
<Что должно измениться>

### Out of Scope
- <Что не делаем сейчас>

### Agents
- `product-agent`: <зачем нужен или "не нужен">
- `backend-agent`: <зачем нужен или "не нужен">
- `frontend-agent`: <зачем нужен или "не нужен">
- `reviewer-agent`: <зачем нужен или "не нужен">

### Contracts To Lock
- <contract 1>
- <contract 2>

### Plan Storage
- Lifecycle: `backlog/` → `active/` → `archive/` (см. `.cursor/plans/README.md`, `.cursor/tasks/README.md`)
- Plan: `.cursor/plans/backlog|active/<plan-id>.md`
- Tasks: `.cursor/tasks/backlog|active/<plan-id>/`

### Parallel Tasks
(Пока план в backlog — те же имена файлов под `.cursor/tasks/backlog/<plan-id>/`; после переноса плана в active — под `tasks/active/`.)

- `product-agent`: `.cursor/tasks/active/<plan-id>/product.md` — parallel: yes/no
- `backend-agent`: `.cursor/tasks/active/<plan-id>/backend.md` — parallel: yes/no
- `frontend-agent`: `.cursor/tasks/active/<plan-id>/frontend.md` — parallel: yes/no
- `reviewer-agent`: `.cursor/tasks/active/<plan-id>/review.md` — after implementation

### Execution Order
1. <step>
2. <step>
3. <step>

### Verification
- <test/build/check>

### Open Questions
- <только blockers>

### Cleanup
- [ ] Task-папка: `active/<plan-id>/` → `archive/<plan-id>/`
- [ ] План: `plans/active/<plan-id>.md` → `plans/archive/<plan-id>.md` (при желании в файле `Status: archived`)
```
