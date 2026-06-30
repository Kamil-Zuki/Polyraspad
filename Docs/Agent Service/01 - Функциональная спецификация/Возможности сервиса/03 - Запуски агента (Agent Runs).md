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
| **SR-AGENT-RUN-02** | **ExecuteRun (orchestrate):** Classify → route → execute tool → CreateRun. |

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

## SR-AGENT-RUN-02: ExecuteRun (orchestrate) {#SR-AGENT-RUN-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Pipeline** | EnsureProjectAccess → Route → Domain gate for LLM tools → ExecuteTool → CreateRun. |
| **Lang context** | `source_lang` / `target_lang` override или из ProjectResponse. |
| **Error softening** | Tool exception → assistant error text, tool status failed, run persisted. |
| **Model tag** | `Ai:Model` если `Ai:Enabled`. |

### 2. Высокоуровневое описание

Представим ExecuteRun как **полный цикл «вопрос → маршрут → инструмент → запись» в одном gRPC-вызове**.

1. **Access & context:** `EnsureProjectAccessAsync` через Vocabulary; `source_lang`/`target_lang` из override или ProjectResponse; archived thread блокируется.
2. **Route & domain gate:** `AgentIntentRouter` выбирает tool по priority; для LLM-tools domain policy должна разрешить категорию, иначе force OutOfScope.
3. **Tool execution:** orchestrator вызывает handler (`explain_word`, `navigate`, …); исключение tool → assistant error text, tool status `failed`, run всё равно persist.
4. **Persist & tag:** результат упаковывается в CreateRun payload; при `Ai:Enabled` run помечается model tag из `Ai:Model`.

Таким образом, PolyGuide chat получает один primary UX path — пользователь пишет текст, сервис сам решает инструмент и сохраняет полный audit trail.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Explain word (Happy Path)

1. User: «Explain the word "slept"».
2. Intent → explain_word; domain allowed.
3. LLM completion + editor draft action in metadata.
4. Persist run; UI показывает explanation + action card.

#### Сценарий Б: Out of scope (Negative Path)

1. User: «Write a C# sorting algorithm».
2. Domain disallowed → out_of_scope tool.
3. Refusal message + suggested prompts; run persisted.

---

*Следующая группа: [[04 - Доменная политика (Domain Policy)]].*
