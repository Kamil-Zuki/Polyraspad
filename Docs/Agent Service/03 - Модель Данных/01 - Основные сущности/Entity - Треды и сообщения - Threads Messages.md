# Entity — Треды и сообщения (Threads & Messages)

## AgentThread

Контейнер диалога пользователя в рамках одного языкового проекта.

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `id` | `uuid` | да | PK, default `uuid_generate_v4()` |
| `user_id` | `uuid` | да | Владелец треда |
| `project_id` | `uuid` | да | Языковой проект (VocabularyService) |
| `title` | `text` | нет | Заголовок; auto-derive из первого user message |
| `agent_id` | `text` | нет | Идентификатор persona агента (например `study-copilot`, `placement-copilot`); фильтр `ListThreads` |
| `system_prompt_override` | `text` | нет | Полный override system prompt для ExecuteRun; если задан — вместо `AgentSystemPromptBuilder` |
| `custom_scenario_id` | `uuid` | нет | FK → `custom_scenarios.id` (nullable); колонка и FK есть, CRUD/API сценариев не wired |
| `created_at` | `timestamptz` | да | default `now()` |
| `updated_at` | `timestamptz` | да | Обновляется при новом run |
| `archived_at` | `timestamptz` | нет | Soft archive; archived исключаются из ListThreads |

**Lifecycle:** create → active (messages/runs) → archive (`archived_at` set). Архивный тред не принимает новые runs.

**SR:** [[01 - Управление тредами (Thread Management)#SR-AGENT-THREAD-01|SR-AGENT-THREAD-01..05]]

---

## AgentMessage

Одно сообщение в треде (user, assistant, system, tool).

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `id` | `uuid` | да | PK |
| `thread_id` | `uuid` | да | FK → `agent_threads` |
| `role` | `varchar(16)` | да | `user` \| `assistant` \| `system` \| `tool` |
| `content` | `text` | да | Текст сообщения |
| `metadata_json` | `jsonb` | нет | Actions, intent, tool metadata для UI |
| `created_at` | `timestamptz` | да | Хронология; assistant +1 ms после user в одном run |

**SR:** [[02 - История сообщений (Message History)#SR-AGENT-MSG-01|SR-AGENT-MSG-01]]
