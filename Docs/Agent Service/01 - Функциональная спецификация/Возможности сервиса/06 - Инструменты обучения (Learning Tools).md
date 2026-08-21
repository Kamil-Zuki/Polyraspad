# Группа 6: Инструменты обучения (Learning Tools)

## Введение

В текущем **ExecuteRun** learning side-effects выполняются через **LLM function-calling tools** (`AvailableTools` в `AgentOrchestrator`) → `ExecuteToolCoreAsync` → Vocabulary gRPC.

**Classic intent IDs** (`explain_word`, `grammar_help`, …) остаются в `AgentIntentRouter` как классификация текста, но **не** являются server-side handlers ExecuteRun. `CustomScenario` — entity reserved без API.

**Метафора:** toolbox репетитора — LLM выбирает «ключ» (deck/card/stats/lesson/placement), а не фиксированный regex→handler pipeline.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к learning tools.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-TOOL-01** | **explain_word (intent id):** Regex intent; ответ через LLM chat, не ExecuteToolCore. |
| **SR-AGENT-TOOL-02** | **grammar_help (intent id):** Regex intent; не ExecuteToolCore handler. |
| **SR-AGENT-TOOL-03** | **generate_example (intent id):** Regex intent; не ExecuteToolCore handler. |
| **SR-AGENT-TOOL-04** | **build_card_draft (intent id):** Regex intent; persist card — tool `create_card`. |
| **SR-AGENT-TOOL-05** | **general_answer (intent id):** Fallback classify; ExecuteRun всегда LLM chat. |
| **SR-AGENT-TOOL-06** | **custom_roleplay (reserved):** `CustomScenario` entity; no gRPC/CRUD/ExecuteRun wiring. |
| **SR-AGENT-TOOL-07** | **create_deck:** LLM tool → CreateDeck. |
| **SR-AGENT-TOOL-08** | **create_card:** LLM tool → CreateCard. |
| **SR-AGENT-TOOL-09** | **get_user_vocabulary_stats:** LLM tool → GetVocabularyStats. |
| **SR-AGENT-TOOL-10** | **get_recent_leeches:** LLM tool → GetLeechCards. |
| **SR-AGENT-TOOL-11** | **mark_lesson_completed:** LLM tool → CompleteLesson. |
| **SR-AGENT-TOOL-12** | **submit_knowledge_check:** LLM tool → SubmitKnowledgeCheckResult. |
| **SR-AGENT-TOOL-13** | **set_cefr_placement:** LLM tool → SetPlacementLevel. |
| **SR-AGENT-TOOL-14** | **get_daily_plan:** LLM tool → GetDailyPlan. |
| **SR-AGENT-TOOL-15** | **generate_writing_task:** LLM tool → GetLearningTerms + instruction. |
| **SR-AGENT-TOOL-16** | **get_skill_assessment_history:** LLM tool → GetSkillAssessmentHistory. |

---

# Детальная спецификация требований

## SR-AGENT-TOOL-01: explain_word (intent id) {#SR-AGENT-TOOL-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Intent only** | `AgentToolId.ExplainWord` в router; **нет** case в `ExecuteToolCoreAsync`. |
| **ExecuteRun** | Объяснение даёт LLM chat (+ optional `OPEN_EDITOR_DRAFT` ACTION line). |

### 2. Высокоуровневое описание

Intent «explain/define/what does» извлекает term для документации/подсказок. Primary path — LLM в ExecuteRun, не отдельный explain_word RPC handler.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Explain «slept» (Happy Path)

1. User: «What does slept mean?»
2. Router → ExplainWord (hint).
3. LLM отвечает в chat; UI может показать draft ACTION.

---

## SR-AGENT-TOOL-02: grammar_help (intent id) {#SR-AGENT-TOOL-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Intent only** | Grammar patterns → `GrammarHelp`; Vocabulary `ExplainGrammar` **не** вызывается из текущего ExecuteRun. |

### 2. Высокоуровневое описание

Грамматические вопросы обрабатывает LLM; делегирование AIService ExplainGrammar в ExecuteRun отсутствует (исторический intent id).

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Grammar question (Happy Path)

1. «Why is went used here?» → GrammarHelp intent.
2. LLM отвечает в рамках language learning prompt.

---

## SR-AGENT-TOOL-03: generate_example (intent id) {#SR-AGENT-TOOL-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Intent only** | Example patterns → `GenerateExample`; `GenerateContext` не вызывается из ExecuteToolCore. |

### 2. Высокоуровневое описание

Запрос примера — LLM chat; writing practice terms — tool `generate_writing_task`.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Example request (Happy Path)

1. «Give me an example with take off» → GenerateExample intent; LLM отвечает.

---

## SR-AGENT-TOOL-04: build_card_draft (intent id) {#SR-AGENT-TOOL-04}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Intent only** | Card patterns → `BuildCardDraft`. |
| **Persist path** | Создание карточки в Vocabulary — LLM tool `create_card` (TOOL-08). |

### 2. Высокоуровневое описание

Regex card intent не создаёт draft handler; LLM может вызвать `create_card` или выдать `OPEN_EDITOR_DRAFT` ACTION.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Card via tool (Happy Path)

1. User просит карточку.
2. LLM → `create_card` с word/translation/deck_id.

---

## SR-AGENT-TOOL-05: general_answer (intent id) {#SR-AGENT-TOOL-05}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Fallback intent** | Unmatched + allowed domain → `GeneralAnswer`. |
| **ExecuteRun** | Всегда LLM `CompleteChatAsync` с system prompt (override или builder). |

### 2. Высокоуровневое описание

Fallback classification не меняет path ExecuteRun — ответ всегда из LLM loop.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Open question (Happy Path)

1. User задаёт учебный вопрос без спец-паттерна.
2. LLM отвечает в рамках PolyGuide prompt.

---

## SR-AGENT-TOOL-06: custom_roleplay (reserved) {#SR-AGENT-TOOL-06}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Entity reserved** | Таблица `custom_scenarios` + FK `agent_threads.custom_scenario_id` существуют. |
| **Not exposed** | Нет gRPC CRUD; CreateThread не принимает scenario id; ExecuteRun не загружает `SystemPromptTemplate`. |
| **No live tool** | Инструмент `custom_roleplay` **не** входит в `AvailableTools`. |

### 2. Высокоуровневое описание

Модель данных подготовлена под ролевые сценарии, но продуктовый path не wired. Документируется как reserved capability (ISSUE-002).

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Gap (Negative Path)

1. Client пытается выбрать CustomScenario в UI.
2. Agent Service API сценариев отсутствует — flow не поддерживается до wiring.

---

## SR-AGENT-TOOL-07: create_deck {#SR-AGENT-TOOL-07}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **LLM tool** | Args: `title` (required), `description` optional. |
| **Vocabulary** | `CreateDeckAsync(user, project, title, desc)`. |

### 2. Высокоуровневое описание

LLM создаёт колоду в проекте; output JSON `{ id, title }` возвращается в tool message.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: New deck (Happy Path)

1. User: «Create a deck Travel phrases».
2. Tool `create_deck` → Vocabulary; assistant подтверждает.

---

## SR-AGENT-TOOL-08: create_card {#SR-AGENT-TOOL-08}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Required args** | `deck_id`, `word`, `translation`; optional `expression`. |
| **Deck fallback** | Invalid/empty deck_id → first deck from `GetDeckTreeAsync`. |

### 2. Высокоуровневое описание

Создаёт flashcard с exact surface form `word` через Vocabulary CreateCard.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Card without deck_id (Happy Path)

1. LLM вызывает `create_card` без валидного deck_id.
2. Orchestrator берёт first root deck; card создаётся.

---

## SR-AGENT-TOOL-09: get_user_vocabulary_stats {#SR-AGENT-TOOL-09}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Read-only** | `GetVocabularyStatsAsync` → counts (total/mature/learning/new). |

### 2. Высокоуровневое описание

LLM запрашивает прогресс словаря для ответа о статистике.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Stats question (Happy Path)

1. User спрашивает о прогрессе.
2. Tool возвращает counts; LLM формулирует ответ.

---

## SR-AGENT-TOOL-10: get_recent_leeches {#SR-AGENT-TOOL-10}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Leeches** | `GetLeechCardsAsync` → id, srs, Word/Translation из note fields. |

### 2. Высокоуровневое описание

Список проблемных карточек для focused practice.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Leech review (Happy Path)

1. User: «Which cards am I failing?»
2. Tool → leech list; LLM предлагает практику.

---

## SR-AGENT-TOOL-11: mark_lesson_completed {#SR-AGENT-TOOL-11}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Args** | `lesson_id` (GUID). |
| **Vocabulary** | `CompleteLessonAsync`. |

### 2. Высокоуровневое описание

Агент отмечает урок завершённым после assessment в диалоге.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Finish lesson (Happy Path)

1. LLM вызывает `mark_lesson_completed` с lesson_id.
2. Vocabulary обновляет статус урока.

---

## SR-AGENT-TOOL-12: submit_knowledge_check {#SR-AGENT-TOOL-12}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Args** | `term_ids` + optional reading/listening/writing/speaking scores 0–100. |
| **Vocabulary** | `SubmitKnowledgeCheckResultAsync`. |

### 2. Высокоуровневое описание

Фиксация результатов knowledge check / skill exam в конце урока.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: End of knowledge check (Happy Path)

1. LLM передаёт term_ids и scores.
2. Vocabulary сохраняет skill assessment.

---

## SR-AGENT-TOOL-13: set_cefr_placement {#SR-AGENT-TOOL-13}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Args** | `cefr_level` (A1..C2). |
| **Vocabulary** | `SetPlacementLevelAsync`. |
| **Loop break** | После successful call ExecuteRun завершает tool loop. |

### 2. Высокоуровневое описание

Placement copilot фиксирует CEFR и unlock curriculum levels.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Placement B1 (Happy Path)

1. Tool `set_cefr_placement` `{ cefr_level: "B1" }`.
2. Loop break; assistant completion message.

---

## SR-AGENT-TOOL-14: get_daily_plan {#SR-AGENT-TOOL-14}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **LLM tool** | `GetDailyPlanAsync` → summary + tasks (fsrs/lesson/knowledge_check). |
| **Greeting inject** | `IsInitialGreeting` (не placement) добавляет plan summary в system prompt без tool call. |

### 2. Высокоуровневое описание

Персональный daily plan для guidance в начале сессии или по запросу LLM.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Init greeting (Happy Path)

1. ExecuteRun с `IsInitialGreeting=true`.
2. Plan summary в system prompt; LLM приветствует и предлагает next step.

---

## SR-AGENT-TOOL-15: generate_writing_task {#SR-AGENT-TOOL-15}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Terms** | `GetLearningTermsAsync` (limit 7) + instruction для writing/translation task. |
| **Follow-up** | Instruction просит затем вызвать `submit_knowledge_check` с writing_score. |

### 2. Высокоуровневое описание

Готовит payload для writing practice на текущих learning terms.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Writing practice (Happy Path)

1. LLM вызывает `generate_writing_task`.
2. Получает terms + instruction; формулирует задание пользователю.

---

## SR-AGENT-TOOL-16: get_skill_assessment_history {#SR-AGENT-TOOL-16}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **History** | `GetSkillAssessmentHistoryAsync` (limit 20) → skill, score, date. |

### 2. Высокоуровневое описание

Тренды reading/listening/writing/speaking для рекомендаций focused practice.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Skill trends (Happy Path)

1. User: «How is my speaking improving?»
2. Tool history → LLM анализирует scores.

---

*Следующая группа: [[07 - Навигация и прогресс (Navigation & Progress)]].*
