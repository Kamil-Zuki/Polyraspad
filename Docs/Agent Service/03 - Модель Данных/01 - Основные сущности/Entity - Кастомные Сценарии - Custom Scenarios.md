# Entity — Кастомные сценарии (Custom Scenarios)

## CustomScenario

Шаблон диалогового сценария (ролевая игра / speaking simulation) для будущего wiring с тредом.

**Таблица:** `internal.custom_scenarios`

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `id` | `uuid` | да | PK |
| `user_id` | `uuid` | нет | Автор; `NULL` — системный/глобальный сценарий |
| `title` | `text` | да | Название (например «В аэропорту») |
| `description` | `text` | нет | Краткое описание ситуации |
| `target_skill` | `text` | да | Целевой навык; default `Speaking` |
| `system_prompt_template` | `text` | да | Шаблон system prompt для роли ИИ |
| `difficulty` | `text` | да | CEFR-сложность (A1..C2) |
| `goals` | `jsonb` | да | Список целей диалога (`List<string>`) |
| `context_configuration` | `text` / jsonb | нет | Доп. настройки окружения |
| `created_at` | `timestamptz` | да | |
| `updated_at` | `timestamptz` | да | |

**Связи:** `AgentThread.custom_scenario_id` → `CustomScenario.id` (FK `fk_agent_threads_custom_scenarios`, 0..1 → N threads).

**Статус реализации:** сущность и миграция присутствуют; **gRPC CRUD / CreateThread binding / ExecuteRun load scenario prompt — не wired**. `CreateThread` не принимает `custom_scenario_id`; orchestrator не читает `CustomScenario` при run. См. [[06 - Инструменты обучения (Learning Tools)#SR-AGENT-TOOL-06|SR-AGENT-TOOL-06]] и ISSUE-002.

**SR:** [[06 - Инструменты обучения (Learning Tools)#SR-AGENT-TOOL-06|SR-AGENT-TOOL-06]] (reserved / not exposed)
