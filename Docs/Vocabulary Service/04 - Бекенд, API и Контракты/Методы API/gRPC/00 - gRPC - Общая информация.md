# Введение

Настоящий документ содержит полное описание **gRPC** интерфейса микросервиса **Vocabulary Service** (порт **5117**, HTTP/2 `h2c`). 

gRPC является основным внутренним контрактом сервиса: его вызывают **AggregatorService** (BFF) для обработки внешних REST-запросов и **AgentService** для работы AI-ассистентов с учебными материалами и терминологией пользователя.

---

# 1. Группы сервисов gRPC (`vocabulary.proto`)

Все RPC-методы микросервиса объявлены в пакете `pvs.content.v1` (`vocabulary.proto`) и разделены по 12 логическим службам:

| Сервис | Назначение | Ключевые методы |
| :--- | :--- | :--- |
| **ContentService** | Ядро управления проектами, глобальными настройками и структурой колод | `CreateProject`, `GetProjects`, `GetProjectDetails`, `UpdateProject`, `GetUserSettings`, `UpdateUserSettings`, `GetDeckTree`, `GetDeckDetail`, `CreateDeck`, `UpdateDeck`, `DeleteDeck` |
| **CardService** | Полный жизненный цикл карточек, заметок, поиска и загрузки медиафайлов | `CreateCard`, `UpdateCard`, `DeleteCard`, `GetCard`, `SearchCards`, `CaptureCard`, `CheckCardDuplicates`, `GetCardsByDeck`, `BulkCreateCards`, `SuspendCard`, `UnsuspendCard`, `UploadImage`, `UploadDocument` |
| **TermService** | Управление точными формами терминов (`ProjectTerm`) и их статусами изученности | `CreateOrUpdateTerm`, `MarkTermKnown`, `IgnoreTerm`, `BulkMarkKnown`, `GetTermDetails`, `SearchTermDuplicates`, `ListProjectTerms`, `PurgeDemoImport` |
| **StudyService** | Управление сессиями FSRS повторений | `StartStudySession`, `GetNextCard`, `SubmitReview`, `UndoReview` |
| **LessonService** | Модуль обучения CEFR (уроки, прогресс, placement, knowledge check) | `GetLessons`, `GetLesson`, `StartLesson`, `CompleteLesson`, `SetPlacementLevel`, `SubmitKnowledgeCheckResult` |
| **SyncService** | Механизм дельта-синхронизации и пакетной отправки ответов | `SyncData`, `BatchSubmitReviews` |
| **AIService** | Вспомогательные AI-инструменты | `GenerateContext`, `ExplainGrammar` |
| **TextService** | Анализ и токенизация текстов ридера на точных формах | `AnalyzeText` |
| **AutonomyService** | Генерация планов автопилота и рекомендаций NBA | `GetDailyAutopilot`, `GetNextBestActions` |
| **SubscriptionService** | Управление подписками пользователя на публичные колоды | `ListSubscriptions`, `Subscribe`, `Unsubscribe` |
| **AnalyticsService** | Статистика и аналитические срезы по навыкам и изучению | `GetDashboardStats`, `GetStudyAnalytics`, `GetSkillRadar` |
| **CommunityService** | Публикация колод в каталог и работа с отзывами | `PublishDeck`, `GetMarketplaceCatalog`, `GetProductDetails`, `CreateReview` |

---

# 2. Архитектура обработки gRPC

1. **Безопасность и контекст:** Входящие gRPC-запросы принимают идентификаторы пользователя (`user_id`) и проекта (`project_id`) в сообщениях запроса или метаданных заголовков.
2. **Обработка ошибок:** Ошибки возвращаются каноническими статус-кодами gRPC (`INVALID_ARGUMENT`, `NOT_FOUND`, `UNAUTHENTICATED`, `PERMISSION_DENIED`, `INTERNAL`).
3. **Внешние вызовы:** Для выполнения алгоритмов FSRS и токенизации NLTK `VocabularyService` выполняет вызовы в микросервис `inclusive` (Python gRPC, порт 40051).
