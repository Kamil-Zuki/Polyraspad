# Основные сущности: Учебный План (Curriculum)

Раздел описывает сущности, отвечающие за пошаговый учебный план, прохождение уроков и оценку навыков пользователя по шкале CEFR.

## 1. Lesson

`Lesson` — это заранее заданный урок в глобальной программе обучения. Уроки разбиты по уровням CEFR и имеют строгую последовательность (в рамках UI, но не жестко в БД, за исключением `UnlocksAfterLessonId`).

**Поля:**
- `Id` (Guid, PK)
- `Title` (string) — название урока (например, "Greetings").
- `Description` (string) — описание урока.
- `CefrLevel` (string) — уровень урока (например, "A1", "B2").
- `OrderIndex` (int) — порядковый номер урока внутри уровня.
- `SystemPromptTemplate` (string) — системный промпт для ИИ, который будет проводить урок.
- `UnlocksAfterLessonId` (Guid?) — если задан, этот урок недоступен до прохождения указанного.

---

## 2. UserLessonProgress

`UserLessonProgress` — связующая сущность, фиксирующая прогресс конкретного пользователя по конкретному уроку.

**Поля:**
- `UserId` (Guid, PK, FK to User)
- `LessonId` (Guid, PK, FK to Lesson)
- `Status` (Enum: `NotStarted`, `InProgress`, `Completed`)
- `ScorePercent` (int) — процент успешности прохождения урока (0-100), заполняется ИИ в конце.
- `TimeSpentSeconds` (int) — сколько секунд пользователь потратил на урок.
- `StartedAt` (DateTime)
- `CompletedAt` (DateTime?)
- `AgentThreadId` (Guid?) — идентификатор чат-сессии в `AgentService`, где проходит урок.

---

## 3. UserCefrProgress

`UserCefrProgress` — агрегированная сущность, отражающая статус конкретного уровня CEFR для пользователя. Обновляется автоматически при завершении уроков (`UpsertCefrProgressAsync`).

**Поля:**
- `UserId` (Guid, PK, FK to User)
- `Level` (string, PK) — уровень (например, "A1").
- `Status` (Enum: `Locked`, `InProgress`, `Completed`) — статус прохождения уровня.
  - `Locked` — предыдущий уровень не пройден, этот закрыт (на уровне UI).
  - `InProgress` — пользователь начал хотя бы один урок уровня.
  - `Completed` — все уроки уровня успешно завершены (Status = Completed).
- `CompletedLessonsCount` (int) — количество завершенных уроков.
- `TotalLessonsCount` (int) — общее количество уроков в уровне.

---

## Связь сущностей

```mermaid
erDiagram
    Lesson ||--o{ UserLessonProgress : tracked_by
    UserLessonProgress }o--|| User(External) : belongs_to
    UserCefrProgress }o--|| User(External) : belongs_to
    
    Lesson }|--|| UserCefrProgress : aggregates_into
```
