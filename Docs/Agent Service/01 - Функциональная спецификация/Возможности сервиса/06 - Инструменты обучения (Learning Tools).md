# Группа 6: Инструменты обучения (Learning Tools)

## Введение

Language-learning tools вызываются из **AgentOrchestrator** после routing. Часть использует **LLM**, часть — **VocabularyService AIService**. Все ответы проходят **SanitizeLemmaLabels**.

**Метафора:** learning tools — **ящик инструментов репетитора**. Объяснение слова, грамматика, пример и карточка — разные «ключи», но все служат одной цели — учить язык точными формами, не леммами.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к learning tools.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-TOOL-01** | **explain_word:** LLM prompt с exact surface form; actions open editor draft / vocabulary. |
| **SR-AGENT-TOOL-02** | **grammar_help:** AIService ExplainGrammarAsync. |
| **SR-AGENT-TOOL-03** | **generate_example:** GenerateContext + translation in response. |
| **SR-AGENT-TOOL-04** | **build_card_draft:** Draft dict Word/Expression/Translation; optional example. |
| **SR-AGENT-TOOL-05** | **general_answer:** PolyGuide system prompt; refuse non-learning in prompt. |

---

# Детальная спецификация требований

## SR-AGENT-TOOL-01: explain_word {#SR-AGENT-TOOL-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Exact form** | Prompt: «exact word or phrase»; no lemma labels. |
| **Missing word** | Error assistant message asking for quoted term. |
| **Actions** | open_editor_draft + navigate vocabulary. |

### 2. Высокоуровневое описание

Представим explain_word как **репетитор, который объясняет точную форму слова, а не лемму из словаря**.

1. **Term check:** orchestrator передаёт extracted word/phrase; если термин отсутствует — assistant просит указать слово в кавычках.
2. **LLM prompt:** `IAgentLlmProvider.CompleteAsync` с system rules «exact word or phrase»; языки из project context (`source_lang`/`target_lang`).
3. **Sanitize:** ответ проходит `SanitizeLemmaLabels` перед persist — UI не показывает legacy lemma labels.
4. **Follow-up actions:** metadata содержит `open_editor_draft` и navigate to vocabulary для продолжения обучения в Reader/Editor.

Таким образом, «slept» объясняется как **slept**, не как лемма sleep, с готовыми UI actions для карточки и словаря.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Explain «slept» (Happy Path)

1. User: «What does slept mean?»
2. Tool объясняет форму **slept**, не лемму sleep.
3. Actions: editor draft + link to vocabulary.

---

## SR-AGENT-TOOL-02: grammar_help {#SR-AGENT-TOOL-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Vocabulary delegate** | Sentence + targetWord + nativeLanguage → ExplainGrammarResponse. |
| **Word required** | Clarification if term missing. |

### 2. Высокоуровневое описание

Представим grammar_help как **грамматический разбор предложения с выделенным словом через отдел словаря**.

1. **Input assembly:** orchestrator собирает sentence, targetWord и nativeLanguage из routed intent и project context.
2. **Vocabulary delegate:** вместо LLM вызывается AIService `ExplainGrammarAsync` с metadata `user_id` + `roles`.
3. **Clarification path:** если targetWord не извлечён — assistant просит указать слово в контексте предложения.
4. **Sanitized output:** ответ форматируется для чата и проходит lemma-label sanitization перед persist.

Таким образом, грамматические объяснения опираются на Vocabulary AIService и сохраняют term-first подход PolyGuide.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Grammar in context (Happy Path)

1. User выделяет слово в предложении и просит объяснить грамматику.
2. gRPC `ExplainGrammarAsync` с sentence + targetWord.
3. Assistant возвращает объяснение без lemma labels.

---

## SR-AGENT-TOOL-03: generate_example {#SR-AGENT-TOOL-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **GenerateContext** | Count=1, UserLevel B1 default. |
| **Output** | Sentence + Translation lines; editor draft action. |

### 2. Высокоуровневое описание

Представим generate_example как **подбор одного учебного предложения с переводом для изучаемой формы**.

1. **Context request:** orchestrator вызывает AIService `GenerateContext` с count=1 и UserLevel B1 по умолчанию для target form.
2. **Dual-line output:** assistant message содержит Sentence + Translation lines для читаемого ответа в PolyGuide.
3. **Editor bridge:** metadata action `open_editor_draft` передаёт точную форму (например, **take off**) в Card Editor.
4. **Persist:** результат сохраняется как обычный run с tool_call record для audit.

Таким образом, пользователь получает контекстный пример и может сразу открыть draft карточки без повторного ввода формы.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Example for phrase (Happy Path)

1. User: «Give me an example with take off».
2. Tool генерирует sentence + translation для фразы **take off**.
3. Action: open_editor_draft с точной формой.

---

## SR-AGENT-TOOL-04: build_card_draft {#SR-AGENT-TOOL-04}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Draft fields** | Word, optional Expression/Translation. |
| **Best-effort example** | GenerateContext optional; failure logged debug only. |

### 2. Высокоуровневое описание

Представим build_card_draft как **черновик карточки на столе редактора — без автосохранения в deck**.

1. **Draft assembly:** orchestrator формирует structured fields Word, optional Expression/Translation из extracted term и LLM/Vocabulary helpers.
2. **Best-effort example:** опциональный `GenerateContext`; failure логируется debug-only — draft всё равно возвращается.
3. **Action metadata:** `open_editor_draft` передаёт payload в Card Editor; пользователь подтверждает save в UI.
4. **Term-first:** Word хранит surface form («slept»), не лемму; card draft готов к LingQ-style workflow.

Таким образом, чат быстро превращает запрос «сделай карточку» в редактируемый draft без mandatory persist в Vocabulary.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Draft from chat (Happy Path)

1. User просит карточку для слова «slept».
2. Draft: Word=slept, Translation=…; optional example если GenerateContext успешен.

---

## SR-AGENT-TOOL-05: general_answer {#SR-AGENT-TOOL-05}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Strict system rules** | Language learning only; no code; term-first. |
| **Refusal detection** | Regex on output → metadata refusal flag. |

### 2. Высокоуровневое описание

Представим general_answer как **универсальный репетитор PolyGuide для вопросов в рамках language learning**.

1. **Strict system prompt:** LLM получает правила «language learning only», no code, term-first — ответы про точные формы, не леммы.
2. **Fallback routing:** intent router направляет сюда unmatched messages при allowed domain после специализированных tools.
3. **Refusal detection:** regex на output LLM может пометить off-domain ответ; metadata `refusal: true` для UI styling.
4. **Sanitize & persist:** ответ санитизируется от lemma labels и сохраняется как completed run с model tag.

Таким образом, PolyGuide закрывает общие учебные вопросы одним fallback tool, не ослабляя domain guardrails.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: sleep vs slept (term-first)

1. User asks about «slept» — tool explains **slept**, not lemma sleep.
2. Card draft uses surface form «slept».

---

*Следующая группа: [[07 - Навигация и прогресс (Navigation & Progress)]].*
