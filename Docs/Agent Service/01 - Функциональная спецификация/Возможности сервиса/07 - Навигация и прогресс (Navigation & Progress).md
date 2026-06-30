# Группа 7: Навигация и прогресс (Navigation & Progress)

## Введение

Product-facing tools помогают пользователю **перейти в нужный раздел** Polyraspad или **увидеть прогресс обучения** без ручного поиска в меню.

**Метафора:** навигация и прогресс — **GPS и дневник тренировок**. Агент не только отвечает на вопрос, но может «отвести» в Reader или Study и показать, сколько уже выучено.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Навигация и прогресс (Navigation & Progress).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-NAV-01** | **navigate tool:** Action cards с href Reader/Editor/Study/Vocabulary/Import/Library. |
| **SR-AGENT-NAV-02** | **get_progress tool:** Streak, daily goals, vocabulary counts из AnalyticsService. |

---

# Детальная спецификация требований

## SR-AGENT-NAV-01: navigate tool {#SR-AGENT-NAV-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Action metadata** | `AgentActionCard`: id, title, kind, href, label, description. |
| **Study href** | `/study/{firstDeckId}` если first_deck_id передан в ExecuteRun. |
| **No LLM** | Static assistant copy «Opening …». |

### 2. Высокоуровневое описание

Представим navigate tool как **GPS-карточку с кнопкой «перейти» в нужный раздел Polyraspad**.

1. **Early match:** IntentRouter распознаёт navigation phrases («Open Reader», «Go to Study») до language tools; domain = `product_navigation`.
2. **Destination resolve:** из intent извлекается Reader/Editor/Study/Vocabulary/Import/Library; Study href = `/study/{firstDeckId}` если `first_deck_id` передан в ExecuteRun.
3. **Static response:** без LLM — assistant copy «Opening …» и `AgentActionCard` в metadata (id, title, kind, href, label, description).
4. **Persist:** run сохраняется с tool_call navigate для audit; UI рендерит action card как кликабельный переход.

Таким образом, пользователь из чата одним кликом попадает в нужный product surface без ручного поиска в меню.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Open Reader (Happy Path)

1. User: «Open the reader».
2. navigate tool → action card href `/reader` (или project-scoped path).
3. Assistant: «Opening Reader…».

---

## SR-AGENT-NAV-02: get_progress tool {#SR-AGENT-NAV-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Dual fetch** | GetDailySummary + GetVocabularyStats parallel conceptually. |
| **Sanitize stats labels** | SanitizeLemmaLabels on progress text (legacy lemma field names in stats DTO). |
| **Follow-up actions** | Navigate Study + Vocabulary. |

### 2. Высокоуровневое описание

Представим get_progress как **дневник тренировок, который агент зачитывает из AnalyticsService**.

1. **Intent match:** «How am I doing?» → `get_progress`; domain category `progress`.
2. **Dual fetch:** orchestrator запрашивает `GetDailySummary` и `GetVocabularyStats` через Vocabulary AnalyticsService с metadata user_id + roles.
3. **Formatted reply:** streak, daily goals, reviews, new cards и term counts собираются в assistant text; stats labels проходят `SanitizeLemmaLabels`.
4. **Follow-up actions:** metadata содержит navigate Study + Vocabulary для продолжения обучения из ответа.

Таким образом, пользователь видит актуальный прогресс проекта в чате и может сразу перейти к study или словарю.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: How am I doing? (Happy Path)

1. User: «How am I doing this week?»
2. Intent get_progress.
3. Assistant lists streak, reviews, new cards, term counts.

---

*Следующая группа: [[08 - Артефакты (Artifacts)]].*
