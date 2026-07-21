# Группа 7: Навигация и прогресс (Navigation & Progress)

## Введение

Product-facing surfaces: **navigate destinations** (intent + ACTION lines в ответе LLM) и **vocabulary progress** (LLM tool `get_user_vocabulary_stats` / related analytics).

**Метафора:** навигация и прогресс — **GPS и дневник тренировок** внутри чата PolyGuide.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Навигация и прогресс (Navigation & Progress).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-NAV-01** | **navigate destinations:** Reader, Editor, Study, Vocabulary, Import, Library, Shadowing, Decks. |
| **SR-AGENT-NAV-02** | **progress / stats:** Vocabulary stats via `get_user_vocabulary_stats` (and related tools). |

---

# Детальная спецификация требований

## SR-AGENT-NAV-01: navigate destinations {#SR-AGENT-NAV-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Destinations** | Reader, Editor, Study, Vocabulary, Import, Library, **Shadowing**, **Decks** (`AgentNavigateDestination`). |
| **Intent match** | `AgentIntentRouter.MatchNavigation` — product_navigation category. |
| **ExecuteRun UI** | Переходы в UI — через строки `NAVIGATE\|path\|title…` в ответе LLM → `AgentActionCard` (нет server-side navigate tool handler). |
| **Study href** | Client/`first_deck_id` на Aggregator/UI стороне для `/study/{deckId}` при необходимости. |

### 2. Высокоуровневое описание

1. **Regex:** «open/go to reader|editor|decks|library|vocab|import|shadow|study…».
2. **LLM:** может эмитить ACTION navigate lines; metadata actions рендерятся как кликабельные cards.
3. **Audit:** navigate не пишется как отдельный ExecuteToolCore name — только если LLM tool loop вызвал другие tools.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Open Decks (Happy Path)

1. User: «Go to my decks».
2. Intent → Navigate/Decks; LLM/ACTION ведёт UI на decks surface.

#### Сценарий Б: Open Shadowing (Happy Path)

1. User: «Practice pronunciation / open shadowing».
2. Destination Shadowing.

---

## SR-AGENT-NAV-02: progress / stats {#SR-AGENT-NAV-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Primary tool** | LLM `get_user_vocabulary_stats` → GetVocabularyStats. |
| **Related** | `get_recent_leeches`, `get_daily_plan`, `get_skill_assessment_history`. |
| **Intent get_progress** | Router всё ещё матчит «how am I doing» → GetProgress; ExecuteRun не вызывает classic get_progress handler. |

### 2. Высокоуровневое описание

Прогресс в чате строится через Vocabulary gRPC tools, которые выбирает LLM, а не через фиксированный get_progress pipeline.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: How am I doing? (Happy Path)

1. User спрашивает о прогрессе.
2. LLM вызывает `get_user_vocabulary_stats` (и при необходимости plan/leeches).
3. Assistant формулирует summary.

---

*Следующая группа: [[08 - Артефакты (Artifacts)]].*
