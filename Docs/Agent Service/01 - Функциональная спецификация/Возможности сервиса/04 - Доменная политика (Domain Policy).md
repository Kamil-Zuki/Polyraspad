# Группа 4: Доменная политика (Domain Policy)

## Введение

PolyGuide **только для language learning**. Domain policy классифицирует user text до вызова LLM-tools и формирует refusal для off-domain запросов (programming, general homework, medical/legal).

**Метафора:** доменная политика — **охранник на входе языковой лаборатории**. Он проверяет, связан ли вопрос с изучением языка; если нет — вежливо не пускает к инструментам и предлагает подходящие темы.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Доменная политика (Domain Policy).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-DOM-01** | **Domain classification:** Regex signals + overrides (learning material in code snippets). |
| **SR-AGENT-DOM-02** | **Out-of-scope refusal:** Templated refusal + RefusalSuggestedPrompts. |

---

# Детальная спецификация требований

## SR-AGENT-DOM-01: Domain classification {#SR-AGENT-DOM-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Categories (Classify)** | `language_learning`, `product_navigation`, `progress`, `out_of_scope` (`AgentDomainPolicy`). |
| **Validator extra** | CreateRun принимает также `automation` (`AgentThreadService.ValidCategories`). |
| **Learning override** | «Explain vocabulary from this code snippet» → allowed language_learning. |
| **Hard block** | Programming implementation patterns → out_of_scope. |
| **ExecuteRun persist** | Текущий ExecuteRun **не** применяет Classify к gate; всегда persist `language_learning` / allowed=true (ISSUE-003). |
| **Persist** | Каждый run сохраняет AgentDomainDecision 1:1. |

### 2. Высокоуровневое описание

Представим domain classification как **пропускной пункт охранника перед языковой лабораторией**.

1. **Input normalize:** `AgentDomainPolicy.Classify` получает user text; navigation/progress intents могут задать category до regex pipeline.
2. **Regex signals:** static patterns распознают `language_learning`, `product_navigation`, `progress` или `out_of_scope`; learning override пропускает vocabulary-in-code snippets.
3. **Hard block:** programming implementation («Build REST API in C#») → `out_of_scope`, `allowed=false`, reason `general_programming_or_non_learning_task`.
4. **LLM gate & persist:** если выбран LLM tool и domain !allowed — orchestrator force OutOfScope; каждый run сохраняет `AgentDomainDecision` 1:1.

Таким образом, PolyGuide не тратит LLM на off-domain запросы и оставляет auditable trace решения для каждого run.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Translate sentence (Happy Path)

1. «Translate this sentence: …» → language_learning, allowed=true.

#### Сценарий Б: Build API backend (Negative Path)

1. «Build a REST API in C#» → out_of_scope, allowed=false, reason `general_programming_or_non_learning_task`.

---

## SR-AGENT-DOM-02: Out-of-scope refusal {#SR-AGENT-DOM-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Code-aware copy** | Отдельный шаблон если user mentions code/C#. |
| **Suggested prompts** | Static array для UI chips. |
| **Refusal flag** | metadata `refusal: true` для styling. |

### 2. Высокоуровневое описание

Представим out-of-scope refusal как **вежливый отказ на входе без вызова переводчика за стеклом**.

1. **Blocked path:** domain `out_of_scope` или LLM gate срабатывает до tool execution — LLM не вызывается.
2. **Templated copy:** `BuildOutOfScopeRefusal` выбирает шаблон; если user упоминает code/C# — отдельный code-aware текст.
3. **Suggested prompts:** static array `RefusalSuggestedPrompts` возвращается для UI chips с подходящими learning topics.
4. **UI metadata:** assistant message помечается `refusal: true` в metadata для styling; run persist через CreateRun как обычный эпизод.

Таким образом, пользователь получает понятный отказ и направление к language-learning сценариям без лишних token costs.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Off-domain request (Negative Path)

1. User: «Write a C# REST API».
2. Domain `out_of_scope` → refusal message + suggested learning prompts.
3. metadata `refusal: true` для UI styling.

---

*Следующая группа: [[05 - Маршрутизация намерений (Intent Routing)]].*
