# Entity — Запуски и аудит (Runs & Audit)

## AgentRun

Один «ход» диалога: пара user+assistant сообщений и связанные tool/domain записи.

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `id` | `uuid` | да | PK |
| `thread_id` | `uuid` | да | FK → `agent_threads` |
| `status` | `varchar(16)` | да | Текущая реализация: `completed` при успешном persist |
| `model` | `text` | нет | LLM model id из `Ai:Model` (если AI enabled) |
| `started_at` | `timestamptz` | да | Начало run |
| `completed_at` | `timestamptz` | нет | Завершение run |
| `error` | `text` | нет | Зарезервировано для failed runs |

**Транзакция:** `CreateRun` сохраняет run, messages, domain decision и tool calls атомарно.

**SR:** [[03 - Запуски агента (Agent Runs)#SR-AGENT-RUN-01|SR-AGENT-RUN-01..02]]

---

## AgentToolCall

Запись вызова инструмента в рамках run.

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `id` | `uuid` | да | PK |
| `run_id` | `uuid` | да | FK → `agent_runs` |
| `tool_name` | `text` | да | Имя tool из LLM loop (e.g. `create_deck`, `get_daily_plan`, `set_cefr_placement`) или client CreateRun payload |
| `input_json` | `jsonb` | да | Вход tool (user text, params) |
| `output_json` | `jsonb` | да | Результат / summary |
| `status` | `varchar(16)` | да | `completed` \| `failed` |
| `created_at` | `timestamptz` | да | |

**SR:** [[05 - Маршрутизация намерений (Intent Routing)#SR-AGENT-INTENT-01|SR-AGENT-INTENT-01]], [[06 - Инструменты обучения (Learning Tools)]]

---

## AgentDomainDecision

Классификация домена запроса для run (1:1 с run).

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `id` | `uuid` | да | PK |
| `run_id` | `uuid` | да | FK UNIQUE → `agent_runs` |
| `allowed` | `boolean` | да | Разрешён ли LLM-tool path |
| `category` | `varchar(32)` | да | `language_learning` \| `product_navigation` \| `progress` \| `out_of_scope` \| `automation` (validator `CreateRun` принимает `automation`; `AgentDomainPolicy.Classify` enum пока без этой категории) |
| `reason` | `text` | нет | Код причины (e.g. `not_language_learning`) |
| `user_text_preview` | `text` | нет | Усечённый preview user message |
| `created_at` | `timestamptz` | да | |

**SR:** [[04 - Доменная политика (Domain Policy)#SR-AGENT-DOM-01|SR-AGENT-DOM-01..02]]
