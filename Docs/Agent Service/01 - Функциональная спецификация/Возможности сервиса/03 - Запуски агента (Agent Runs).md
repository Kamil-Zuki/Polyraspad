# Группа 3: Запуски агента (Agent Runs)

## Введение

**Run** — атомарная единица диалога: user message, assistant reply, domain audit, tool call records. Два входа: **CreateRun** (готовый payload) и **ExecuteRun** (полный server pipeline).

**Метафора:** run — **записанный эпизод разговора**. Один вопрос пользователя и ответ ассистента сохраняются как единый блок в журнале, вместе с метаданными «какой инструмент вызывали и почему».

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Запуски агента (Agent Runs).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-RUN-01** | **CreateRun (persist):** Transaction: messages + run + domain + tool_calls; auto title derive. |
| **SR-AGENT-RUN-02** | **ExecuteRun (LLM tool loop):** Prompt + history → LLM tools → ExecuteToolCore → CreateRun. |

---

# Детальная спецификация требований

## SR-AGENT-RUN-01: CreateRun (persist) {#SR-AGENT-RUN-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Atomic TX** | EF transaction на все inserts. |
| **Validation** | Roles: user/assistant/system/tool; categories domain; tool status completed/failed. |
| **Title derive** | Первый run без title → `DeriveTitle(user content)`. |
| **Archived guard** | Run на archived thread → InvalidOperation → FailedPrecondition. |
| **Project match** | `thread.project_id == request.project_id`. |

### 2. Высокоуровневое описание

Представим CreateRun как **запись готового эпизода в журнал диалога одной транзакцией**.

1. **Pre-checks:** thread принадлежит user, не archived, `thread.project_id` совпадает с request; roles и categories валидируются до insert.
2. **Atomic persist:** EF transaction создаёт user/assistant messages, run row, `AgentDomainDecision` 1:1 и tool_call records.
3. **Title derive:** если у thread ещё нет title — первый user content проходит через `DeriveTitle` и обновляет thread.
4. **Immediate completion:** при успехе run status = `completed`; response возвращает run + message items для UI.

Таким образом, ExecuteRun и advanced client flows делят один надёжный persist path — история диалога не остаётся в полусохранённом состоянии.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Successful persist (Happy Path)

1. Valid CreateAgentRunRequest.
2. DB commit.
3. Response: run + user_message + assistant_message items.

---

## SR-AGENT-RUN-02: ExecuteRun (LLM tool loop) {#SR-AGENT-RUN-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Primary path** | EnsureProjectAccess → load history → build system prompt → LLM `CompleteChatAsync` с `AvailableTools` (до 5 loops) → `ExecuteToolCoreAsync` → CreateRun. |
| **System Prompt** | `SystemPromptOverride` из треда **или** `AgentSystemPromptBuilder.Build(agent_id, project, langs)`. |
| **Intent hint (limited)** | `AgentIntentRouter.Route` вызывается; **только** `GeneratePractice` дополняет system prompt learning terms. `IsInitialGreeting` (не placement) injects daily plan summary. |
| **Domain persist** | Текущий ExecuteRun всегда пишет `AgentDomainDecision` как `allowed=true`, `language_learning` (не результат Classify). |
| **Lang context** | `source_lang` / `target_lang` override или из ProjectResponse. |
| **Error softening** | Tool exception → JSON `{ error }` в tool message, status `failed`; loop продолжается; run persist. |
| **Model tag** | `Ai:Model` если `Ai:Enabled`. |
| **Not classic pipeline** | Server-side handlers `explain_word` / `navigate` / `grammar_help` **не** вызываются из ExecuteRun (см. ISSUE-003). |

### 2. Высокоуровневое описание

Представим ExecuteRun как **цикл «вопрос → LLM с toolbox → вызовы Vocabulary → запись»**.

1. **Access & context:** `EnsureProjectAccessAsync`; history последних user/assistant messages; prompt из override или builder по `agent_id`.
2. **Hints:** при intent `GeneratePractice` — список learning terms в system prompt; при `IsInitialGreeting` — daily plan summary (кроме `placement-copilot`).
3. **LLM tool loop:** `CompleteChatAsync(systemPrompt, messages, AvailableTools)`; при tool_calls — `ExecuteToolCore` (`create_deck`, `create_card`, `get_daily_plan`, …) и append tool messages; max 5 итераций; `set_cefr_placement` success может оборвать loop.
4. **UI actions:** строки `NAVIGATE|…` / `OPEN_EDITOR_DRAFT|…` в ответе LLM парсятся в `AgentActionCard` metadata.
5. **Persist:** CreateRun с tool_call records и hardcoded domain `language_learning`.

Таким образом, primary UX path — LLM function calling, а не regex→classic tool dispatch.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Create card via LLM tool (Happy Path)

1. User: «Create a card for slept / спал».
2. LLM вызывает `create_card` → Vocabulary CreateCard.
3. Persist run с tool_call `create_card`; assistant summary.

#### Сценарий Б: Placement completion (Happy Path)

1. Placement agent вызывает `set_cefr_placement` с `cefr_level=B1`.
2. Orchestrator → `SetPlacementLevelAsync`; loop break.
3. Run persisted; UI показывает completion copy.

---

*Следующая группа: [[04 - Доменная политика (Domain Policy)]].*
