# REST API: Уроки и прогресс (Lessons)

Данный документ описывает публичный REST-контракт контроллера `LessonsController` (`/api/lessons` и `/api/projects/{projectId}/lessons`) в **AggregatorService**, проксирующего вызовы в gRPC-сервис `LessonService` микросервиса `VocabularyService`.

**SR группы:** SR-AGG-LESSON-01 ... SR-AGG-LESSON-06

---

## 1. Перечень Эндпоинтов

| Метод | Эндпоинт | gRPC Метод | Описание |
| :---: | :--- | :--- | :--- |
| `GET` | `/api/lessons` | `LessonService.GetLessons` | Получение списка всех уроков с прогрессом текущего пользователя |
| `GET` | `/api/lessons/{id}` | `LessonService.GetLesson` | Детали конкретного урока |
| `POST` | `/api/lessons/{id}/start` | `LessonService.StartLesson` | Начало урока с генерацией `AgentThreadId` |
| `POST` | `/api/lessons/{id}/complete` | `LessonService.CompleteLesson` | Фиксация завершения урока и итогового балла |
| `POST` | `/api/lessons/placement` | `LessonService.SetPlacementLevel` | Прохождение Placement Test с простановкой CEFR |
| `POST` | `/api/lessons/knowledge-check` | `LessonService.SubmitKnowledgeCheckResult` | Отправка результатов проверки знаний |

---

## 2. Спецификация Вызовов

### `GET /api/lessons` (SR-AGG-LESSON-01)
- **Авторизация:** JWT Bearer
- **Downstream gRPC:** `LessonService.GetLessons(GetLessonsRequest { user_id })`
- **Успешный ответ (200 OK):**
  ```json
  [
    {
      "lesson": {
        "id": "lesson-uuid-001",
        "title": "Basic Greetings",
        "category": "Grammar",
        "difficulty": "A1",
        "contentMarkdown": "# Greetings...",
        "cefrLevel": "A1",
        "orderIndex": 1,
        "estimatedMinutes": 15
      },
      "progress": {
        "status": 2,
        "scorePercent": 95,
        "timeSpentSeconds": 600,
        "completedAt": "2026-08-01T12:00:00Z"
      }
    }
  ]
  ```

### `POST /api/lessons/{id}/start` (SR-AGG-LESSON-03)
- **Авторизация:** JWT Bearer
- **Downstream gRPC:** `LessonService.StartLesson`
- **Запрос:** `{ "agentThreadId": "optional-thread-uuid" }`
- **Успешный ответ (200 OK):** `UserLessonProgressDto`

### `POST /api/lessons/placement` (SR-AGG-LESSON-05)
- **Авторизация:** JWT Bearer
- **Downstream gRPC:** `LessonService.SetPlacementLevel`
- **Запрос:** `{ "cefrLevel": "B1" }`
- **Успешный ответ (200 OK):** `200 OK` (уровни ниже B1 помечаются пройденными).
