# Введение

Настоящий документ определяет **внешний REST API контракт** публичного слоя **AggregatorService (BFF)** для работы с возможностями микросервиса **Vocabulary Service**.

По целевой архитектуре `VocabularyService` является внутренним доменным микросервисом (gRPC порт **5117**). Публичный REST API экспонируется исключительно через **AggregatorService** (порт **5000**), который обрабатывает HTTP-запросы от SPA (Next.js), Chrome-расширения и мобильных приложений, конвертирует DTO и проксирует вызовы по gRPC.

---

# 1. Сводка групп эндпоинтов REST API Aggregator BFF

| Группа | Префикс URI | Назначение | Underlying gRPC |
| :--- | :--- | :--- | :--- |
| **Проекты и Настройки** | `/api/v1/projects`, `/api/v1/settings` | Управление языковыми пространствами и целями | `ContentService.CreateProject`, `GetProjects`, `UpdateUserSettings` |
| **Колоды (Decks)** | `/api/v1/decks` | Иерархическое дерево колод, детали, создание и редактирование | `ContentService.GetDeckTree`, `GetDeckDetail`, `CreateDeck` |
| **Карточки и Заметки** | `/api/v1/cards` | Создание, редактирование, поиск, захват из расширения, медиа | `CardService.CreateCard`, `CaptureCard`, `SearchCards` |
| **Термины Словаря** | `/api/v1/terms` | Изменение статуса слов (`SAVED`, `KNOWN`, `IGNORED`), массовая пометка | `TermService.MarkTermKnown`, `BulkMarkKnown`, `ListProjectTerms` |
| **Повторения FSRS** | `/api/v1/study` | Старт сессии повторений, получение следующей карточки, отправка оценки | `StudyService.StartStudySession`, `GetNextCard`, `SubmitReview` |
| **Учебный План (CEFR)** | `/api/v1/lessons` | Прохождение уроков, placement test, проверочные тесты | `LessonService.GetLessons`, `SetPlacementLevel`, `CompleteLesson` |
| **Синхронизация** | `/api/v1/sync` | Офлайн дельта-синхронизация и пакетная отправка ответов | `SyncService.SyncData`, `BatchSubmitReviews` |
| **Marketplace** | `/api/v1/marketplace` | Каталог колод, публикация, отзывы, подписки | `CommunityService.GetMarketplaceCatalog`, `SubscriptionService.Subscribe` |

---

# 2. Безопасность и Заголовки Контекста

Входящие HTTP-запросы на AggregatorService валидируются через JWT Bearer токен (`Authorization: Bearer <JWT>`). AggregatorService извлекает `UserId` и проксирует его в gRPC-запросах к `VocabularyService`.
