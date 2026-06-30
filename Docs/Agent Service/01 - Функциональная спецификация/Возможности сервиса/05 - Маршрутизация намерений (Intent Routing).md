# Группа 5: Маршрутизация намерений (Intent Routing)

## Введение

**AgentIntentRouter** преобразует свободный user text в **RoutedAgentIntent** (tool id + extracted word/sentence/destination). Regex-based, без ML — предсказуемо и тестируемо.

**Метафора:** intent router — **диспетчерская с табло направлений**. Свободная реплика пользователя превращается в конкретный «маршрут» — какой инструмент вызвать и с какими параметрами.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Маршрутизация намерений (Intent Routing).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-INTENT-01** | **Intent routing:** Priority order navigation → progress → grammar → example → card → explain → general/out_of_scope. |

---

# Детальная спецификация требований

## SR-AGENT-INTENT-01: Intent routing {#SR-AGENT-INTENT-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **ToolName mapping** | AgentToolId → snake_case (`explain_word`, `get_progress`, …). |
| **Term extraction** | Quoted strings, «word X», explain/define patterns, card for patterns. |
| **Navigation destinations** | Reader, Editor, Study, Vocabulary, Import, Library. |
| **Fallback** | Unmatched allowed domain → general_answer; disallowed → out_of_scope. |

### 2. Высокоуровневое описание

Представим intent routing как **диспетчерскую с табло: свободная реплика → конкретный маршрут и параметры**.

1. **Normalize:** `Route()` приводит user text к единому виду и проверяет navigation/progress patterns раньше language tools.
2. **Priority order:** navigation → progress → grammar → example → card → explain → general/out_of_scope; первое совпадение определяет `AgentToolId`.
3. **Term extraction:** quoted strings, «word X», explain/define и card-for patterns извлекают word/sentence/destination для handler.
4. **Fallback:** unmatched при allowed domain → `general_answer`; при disallowed → `out_of_scope`; domain category может быть embedded в intent (navigation, progress).

Таким образом, PolyGuide получает предсказуемый regex-based routing без ML — каждый user message мапится в testable `RoutedAgentIntent`.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Open Reader (Happy Path)

1. «Open Reader» → Navigate, destination Reader, category product_navigation.

#### Сценарий Б: Grammar question (Happy Path)

1. «Why is "went" used here?» → GrammarHelp, word extracted.

---

*Следующая группа: [[06 - Инструменты обучения (Learning Tools)]].*
