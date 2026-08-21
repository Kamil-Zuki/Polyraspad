# Entity — Артефакты (Artifacts)

## AgentArtifact

Структурированный результат run (draft карточки, navigation payload и т.д.) для повторного использования в UI.

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `id` | `uuid` | да | PK |
| `run_id` | `uuid` | да | FK → `agent_runs` |
| `thread_id` | `uuid` | да | FK → `agent_threads` (денormalized для list by thread) |
| `kind` | `varchar(32)` | да | Тип артефакта (domain-specific string) |
| `payload_json` | `jsonb` | да | JSON payload |
| `created_at` | `timestamptz` | да | |

**Инварианты:**

- `run_id` должен принадлежать тому же `thread_id`, что указан в запросе.
- Доступ только если thread owned by `user_id`.

**SR:** [[08 - Артефакты (Artifacts)#SR-AGENT-ART-01|SR-AGENT-ART-01..02]]
