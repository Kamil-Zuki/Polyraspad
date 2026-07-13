# Контракты gRPC (Vocabulary Service)

`VocabularyService` предоставляет gRPC API для `AggregatorService` и `AgentService`.
Все контракты описаны в файле `vocabulary.proto`.

## 1. VocabularyService (Core)
Сервис для управления карточками, колодами и словарем.

- `CreateDeck`, `UpdateDeck`, `DeleteDeck`
- `CreateCard`, `UpdateCard`, `DeleteCard`
- `GetVocabularyStats`, `GetLeechCards`
- И другие CRUD-операции.

## 2. StudyService
Сервис для управления сессиями интервального повторения.

- `StartSession(StartSessionRequest)` — инициализация очереди карточек на сегодня.
- `ReviewCard(ReviewCardRequest)` — отправка ответа (рейтинга) и пересчет FSRS-статуса.
- `UndoReview(UndoReviewRequest)` — отмена последнего ответа.

## 3. LessonService
Сервис для управления программой обучения (Curriculum) и CEFR.

- `GetLessons(GetLessonsRequest)` — получить список всех уроков и текущий прогресс пользователя (включая `CefrProgress`).
- `GetLesson(GetLessonRequest)` — получить один урок с прогрессом.
- `StartLesson(StartLessonRequest)` — начать урок, переводит статус в `InProgress`, сохраняет `AgentThreadId`.
- `CompleteLesson(CompleteLessonRequest)` — завершить урок.
- `SetPlacementLevel(SetPlacementLevelRequest)` — **Placement Test**: проставляет статус `Completed` (score = 100%) всем урокам ниже указанного уровня CEFR и пересчитывает `UserCefrProgress`.
- `SubmitKnowledgeCheckResult(SubmitKnowledgeCheckResultRequest)` — отправляет результаты теста навыков (R/L/W/S) по определенным карточкам.

## 4. Зависимости (Исходящие вызовы)
- Вызывает микросервис `inclusive` (Python) через `vocab.proto` для токенизации текста и расчета интервалов FSRS.
- Вызывает `MediaService` для получения presigned-ссылок на аудио и изображения.
