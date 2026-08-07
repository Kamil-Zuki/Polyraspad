# REST API: Автопилот дня (Autopilot)

Данный документ описывает публичный REST-контракт контроллера `AutopilotController` (`/api/autopilot` и `/api/projects/{projectId}/autopilot`) в **AggregatorService**, проксирующего вызовы в `AutonomyService` и `LessonService` микросервиса `VocabularyService`.

**SR группы:** SR-AGG-AUTOPILOT-01 ... SR-AGG-AUTOPILOT-02

---

## 1. Перечень Эндпоинтов

| Метод | Эндпоинт | gRPC Метод | Описание |
| :---: | :--- | :--- | :--- |
| `GET` | `/api/autopilot/plan` | `AutonomyService.GetDailyAutopilot` | Получение расчитанного плана автопилота на текущий день |
| `GET` | `/api/autopilot/next-actions` | `AutonomyService.GetNextBestActions` | Приоритетные действия (Next Best Actions) для пользователя |
| `POST` | `/api/autopilot/track-skill` | `LessonService.TrackSkillActivity` | Учет ежедневной активности по конкретному навыку |

---

## 2. Спецификация Вызовов

### `GET /api/autopilot/plan?projectId={projectId}` (SR-AGG-AUTOPILOT-01)
- **Авторизация:** JWT Bearer
- **Downstream gRPC:** `AutonomyService.GetDailyAutopilot`
- **Успешный ответ (200 OK):**
  ```json
  {
    "userId": "user-uuid",
    "projectId": "project-uuid",
    "planDate": "2026-08-07",
    "suggestedMinutes": 30,
    "suggestedNewCards": 20,
    "suggestedReviews": 100,
    "backlogRiskScore": 15,
    "sessionMode": "Standard",
    "nextBestActions": [
      {
        "id": "nba-001",
        "type": "study",
        "title": "Review Due Cards",
        "description": "You have 45 cards waiting for review",
        "priority": 1
      }
    ]
  }
  ```

### `POST /api/autopilot/track-skill` (SR-AGG-AUTOPILOT-02)
- **Авторизация:** JWT Bearer
- **Downstream gRPC:** `LessonService.TrackSkillActivity`
- **Запрос:**
  ```json
  {
    "projectId": "project-uuid",
    "skillTypeId": 1,
    "value": 15
  }
  ```
- **Успешный ответ (200 OK):**
  ```json
  {
    "totalValueToday": 30,
    "isCompleted": true
  }
  ```
