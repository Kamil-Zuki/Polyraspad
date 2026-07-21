# Группа 13: Автоматизация (Automation)

## Введение

В этом разделе описывается REST-слой Aggregator Service для **study copilot feedback** и **A/B experiments**. Текущая реализация — **stub/no-op**: copilot возвращает пустой neutral feedback; experiments всегда `control`; events только Debug-log.

Endpoints существуют, чтобы frontend **не получал 404** и не блокировал study session при включённых feature flags.

**Метафора:**

Представьте **заглушки на приборной панели**, пока инженеры монтируют настоящие датчики. UI уже вызывает endpoints; панель показывает «нет данных», но самолёт (study flow) продолжает лететь.

См. [[99 - Staging — Разрывы согласованности (DO NOT DELETE)/ISSUE-001-copilot-stub|ISSUE-001]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к automation (stub).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-AUTO-01** | **Copilot feedback после review (stub):** Контрактный placeholder после FSRS review — neutral empty response без вызова LLM. |
| **SR-AGG-AUTO-02** | **A/B experiments (stub):** Стабильный control variant и no-op event tracking — frontend не блокируется при включённых flags. |

---

# Детальная спецификация требований

## SR-AGG-AUTO-01: Copilot review feedback (stub) {#SR-AGG-AUTO-01}

После FSRS review UI может запросить LLM explanation — endpoint существует, но **не вызывает LLM**; возвращает empty neutral DTO.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **JWT** | `AutomationController` — `[Authorize]`. |
| **cardId required** | Missing/empty cardId → HTTP **400**. |
| **Stub response** | tone=`neutral`, empty strings, no remedial cards. |
| **Future** | `IStudyCopilotFeedbackService` + AI completion (ISSUE-001). |
| **No gRPC** | Локальный return без downstream call. |

### 2. Высокоуровневое описание

Представим stub как **манекен консультанта на стойке Study**.

1. **Study UI** POST feedback после review rating.
2. **Aggregator** валидирует cardId в body.
3. **Immediate return** placeholder `CopilotReviewFeedbackDto` — no LLM latency.
4. **UI** скрывает copilot panel или показывает empty state.

Planned: real implementation via OpenAI-compatible API (см. `Ai:*` config) + card context from Vocabulary.

Таким образом, **contract stability** опережает **feature completeness**.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** POST `/api/automation/copilot/review-feedback`.
* **Body:** `CopilotReviewFeedbackRequestDto` (cardId, optional context).

#### Сценарий А: Post-review feedback (Stub Path)

**Сценарий:** Study UI запрашивает copilot hint после Good rating.

1. **POST** with valid cardId + Bearer JWT.
2. **BFF:** no external call.
3. **Ответ:** HTTP **200**, empty neutral `CopilotReviewFeedbackDto`.

#### Сценарий Б: Missing cardId (Negative Path)

1. **POST** with null body or empty cardId.
2. **Ответ:** HTTP **400** `{ "error": "Нужен корректный cardId." }`.

---

## SR-AGG-AUTO-02: Experiment assignment and events {#SR-AGG-AUTO-02}

Feature flags / A/B без внешнего experiment platform — stable `control` variant.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Stable control** | GET assignment → variant `"control"` always. |
| **No-op track** | POST events → **204**, Debug log only. |
| **key required** | Query/body validation on key, variant, eventName. |
| **JWT on controller** | Class-level `[Authorize]`. |
| **Frontend unblock** | Prevents 404 blocking study when experiments UI enabled. |

### 2. Высокоуровневое описание

Представим experiments как **переключатель, зафиксированный в положении A**.

1. **Frontend** on study start requests assignment for experiment key (e.g. `study-copilot`).
2. **BFF** returns `{ key, variant: "control" }` — default UX path.
3. **Events** POST for future analytics — logged Debug, not persisted.
4. **Later:** external experiment service or Vocabulary analytics pipeline.

Таким образом, **experiment hooks** wired без **experiment infrastructure**.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Get assignment (Happy Path)

**Сценарий:** Study session bootstrap requests experiment variant.

1. **GET** `/api/automation/experiments/assignment?key=study-copilot`.
2. **Ответ:** HTTP **200**, `{ key: "study-copilot", variant: "control" }`.

#### Сценарий Б: Track event (No-op Path)

1. **POST** `/api/automation/experiments/events` with key, variant, eventName.
2. **BFF:** Debug log.
3. **Ответ:** HTTP **204 No Content**.

#### Сценарий В: Missing key (Negative Path)

1. **GET** assignment without key query.
2. **Ответ:** HTTP **400**.

---

## SR-AGG-AUTO-03: Automation jobs {#SR-AGG-AUTO-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| In-memory | `InMemoryAutomationJobOrchestrator` — не Postgres |
| REST | POST/GET `/api/automation/jobs` |

### 2. Высокоуровневое описание
Клиент создаёт job и опрашивает статус; состояние живёт в процессе Aggregator (теряется при рестарте).

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Create + poll
1. POST job → jobId.
2. GET job → status/result.

---

*Следующая группа: [[14 - Внешние интеграции (Integrations)]].*
