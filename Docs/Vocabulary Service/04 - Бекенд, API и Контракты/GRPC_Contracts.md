# Контракты gRPC (Vocabulary Service)

`VocabularyService` предоставляет gRPC API на порту **5117** (`h2c` HTTP/2) для клиентов `AggregatorService` (BFF) и `AgentService`.
Основной контракт сервиса описан в `.proto`-файле `vocabulary.proto` (package `pvs.content.v1`, C# namespace `Pvs.Content.Grpc`).

---

## 1. Обзор gRPC Сервисов

| Сервис | Описание и Основные RPC | Требования |
| :--- | :--- | :--- |
| **ContentService** | Проекты (`CreateProject`, `GetProjects`, `GetProjectDetails`, `UpdateProject`), настройки (`GetUserSettings`, `UpdateUserSettings`), колоды (`GetDeckTree`, `GetDeckDetail`, `CreateDeck`, `UpdateDeck`, `DeleteDeck`). | SR-VOC-04, SR-SETT-01 |
| **CardService** | Создание и редактирование карточек (`CreateCard`, `UpdateCard`, `DeleteCard`, `GetCard`), поиск (`SearchCards`), забор из расширения (`CaptureCard`), проверка дубликатов (`CheckCardDuplicates`), редактирование заметок, загрузка медиа (`UploadImage`, `UploadDocument`). | SR-VOC-01, SR-VOC-04 |
| **TermService** | Управление терминами проекта (`CreateOrUpdateTerm`, `MarkTermKnown`, `IgnoreTerm`, `BulkMarkKnown`, `GetTermDetails`, `SearchTermDuplicates`, `ListProjectTerms`, `PurgeDemoImport`). | SR-VOC-05 |
| **StudyService** | Управление сессиями FSRS повторений (`StartStudySession`, `GetNextCard`, `SubmitReview`, `UndoReview`). | SR-VOC-02 |
| **LessonService** | Управление CEFR уроками (`GetLessons`, `GetLesson`, `StartLesson`, `CompleteLesson`, `SetPlacementLevel`, `SubmitKnowledgeCheckResult`). | SR-VOC-01 |
| **SyncService** | Офлайн-синхронизация (`SyncData`, `BatchSubmitReviews`). | SR-VOC-08 |
| **AIService** | AI-инструменты (`GenerateContext`, `ExplainGrammar`). | SR-VOC-06 |
| **TextService** | NLP-анализ и токенизация текста (`AnalyzeText`). | SR-VOC-05 |
| **AutonomyService** | Автопилот и рекомендации (`GetDailyAutopilot`, `GetNextBestActions`). | SR-VOC-06 |
| **SubscriptionService** | Подписки на публичные колоды (`ListSubscriptions`, `Subscribe`, `Unsubscribe`). | SR-VOC-07 |
| **AnalyticsService** | Статистика и аналитика (`GetDashboardStats`, `GetStudyAnalytics`, `GetSkillRadar`). | SR-VOC-06 |
| **CommunityService** | Публикация колод и отзывы (`PublishDeck`, `GetMarketplaceCatalog`, `GetProductDetails`, `CreateReview`). | SR-VOC-07 |

---

## 2. Зависимости (Исходящие gRPC вызовы)

- **`inclusive` (Python gRPC, порт 40051):** Вызов `vocab.proto` (`ReviewCard` для FSRS расчетов, `Tokenize`/`Lemmatize` для NLTK токенизации).
- **`MediaService` (gRPC, порт 5121):** Получение presigned-ссылок на аудио и изображения.
- **`BillingService` (gRPC, порт 5127):** Проверка прав и лимитов подписок на платные колоды (`UserEntitlement`).
