# Введение

Study copilot feedback (stub) и A/B experiments (stub/no-op). JWT. Локальная логика на BFF без gRPC downstream. См. ISSUE-001 в staging.

DTO: `CopilotReviewFeedbackRequestDto`, `CopilotReviewFeedbackDto`, `ExperimentAssignmentDto`, `TrackExperimentEventDto` — `AggregatorService/Dtos/AutomationDtos.cs`.

# 1. Список эндпоинтов

Сверено с `AggregatorService/Controllers/AutomationController.cs`.

| SR | Method | Route | Поведение |
| :--- | :--- | :--- | :--- |
| SR-AGG-AUTO-01 | POST | `/api/automation/copilot/review-feedback` | Neutral empty stub (200) |
| SR-AGG-AUTO-02 | GET | `/api/automation/experiments/assignment?key=` | Always `variant=control` |
| SR-AGG-AUTO-02 | POST | `/api/automation/experiments/events` | No-op, Debug log, **204** |

---

# SR-AGG-AUTO-01: Copilot review feedback: POST /api/automation/copilot/review-feedback

## Общая информация

Placeholder после FSRS review; **не вызывает LLM** (stub до ISSUE-001).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | CopilotReviewFeedbackRequestDto (`cardId` required) |
| **DTO успешного ответа** | CopilotReviewFeedbackDto |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* JWT required
* Validate `cardId` in body
* Return fixed DTO: `tone=neutral`, empty strings, `suggestRemedialCards=false`

## Успешный ответ

HTTP **200**:

```json
{
  "tone": "neutral",
  "explanation": "",
  "actionHint": "",
  "suggestRemedialCards": false,
  "remedialCards": []
}
```

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing/invalid cardId |
| **401** | JWT |

---

# SR-AGG-AUTO-02: Experiment assignment: GET /api/automation/experiments/assignment

## Общая информация

Стабильный control variant для UI feature flags.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | Query: `key` (required) |
| **DTO успешного ответа** | ExperimentAssignmentDto |

## Логика обработки запроса

* Always `{ key, variant: "control" }`

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing key |
| **401** | JWT |

---

# SR-AGG-AUTO-02: Track event: POST /api/automation/experiments/events

## Общая информация

No-op telemetry; предотвращает 404 на frontend.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | TrackExperimentEventDto (`key`, `variant`, `eventName` required) |
| **DTO успешного ответа** | N/A |

## Логика обработки запроса

* Log Debug, return **204 No Content**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing key/variant/eventName |
| **401** | JWT |
