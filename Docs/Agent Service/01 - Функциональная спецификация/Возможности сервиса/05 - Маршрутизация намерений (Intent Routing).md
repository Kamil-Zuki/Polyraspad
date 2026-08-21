# Группа 5: Маршрутизация намерений (Intent Routing)

## Введение

**AgentIntentRouter** преобразует свободный user text в **RoutedAgentIntent** (tool id + extracted word/sentence/destination). Regex-based, без ML.

В текущем **ExecuteRun** router — **hint layer**: результат используется для `GeneratePractice` (inject terms) и для документации destinations; classic tool handlers **не** диспатчатся (см. [[03 - Запуски агента (Agent Runs)#SR-AGENT-RUN-02|SR-AGENT-RUN-02]], ISSUE-003).

**Метафора:** intent router — **табло подсказок**, а не единственный селектор пути ExecuteRun.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Маршрутизация намерений (Intent Routing).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-INTENT-01** | **Intent routing:** Priority navigation → progress → grammar → example → practice → card → explain → get_daily_plan → general/out_of_scope. |

---

# Детальная спецификация требований

## SR-AGENT-INTENT-01: Intent routing {#SR-AGENT-INTENT-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **ToolName mapping** | AgentToolId → snake_case (`explain_word`, `generate_practice`, `get_daily_plan`, `navigate`, …). |
| **Term extraction** | Quoted strings, «word X», explain/define patterns, card for patterns. |
| **Navigation destinations** | Reader, Editor, Study, Vocabulary, Import, Library, **Shadowing**, **Decks**. |
| **GeneratePractice** | Patterns `practice` / `test me` / `упражнение` → intent; ExecuteRun добавляет learning terms в system prompt. |
| **GetDailyPlan** | Patterns «что делаем сегодня» / start/begin → intent id; daily plan в ExecuteRun также через tool / `IsInitialGreeting`. |
| **Fallback** | Unmatched → Classify; disallowed → out_of_scope; allowed → general_answer. |
| **ExecuteRun scope** | Кроме GeneratePractice (+ greeting plan) intents **не** выбирают server-side classic handlers. |

### 2. Высокоуровневое описание

1. **Normalize:** `Route()` приводит user text к lower и проверяет navigation/progress раньше language tools.
2. **Priority order:** navigation → progress → grammar → example → **generate_practice** → card → explain → **get_daily_plan** → Classify → general/out_of_scope.
3. **Destinations:** Shadowing (`shadow` / pronunciation) и Decks (`my decks`) входят в `MatchNavigation`.
4. **ExecuteRun:** только `GeneratePractice` меняет prompt; остальное обрабатывает LLM tool loop.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Open Shadowing (Happy Path)

1. «Open shadowing» → Navigate, destination Shadowing, category product_navigation.

#### Сценарий Б: Practice request (Happy Path)

1. «Test me» → GeneratePractice.
2. ExecuteRun подмешивает learning terms в system prompt; LLM ведёт упражнение / может вызвать `generate_writing_task`.

---

*Следующая группа: [[06 - Инструменты обучения (Learning Tools)]].*
