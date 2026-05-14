# Аудит Aggregator REST Bridge для Reader

**Дата аудита:** 2026-05-13  
**Статус:** Критические разрывы выявлены

## Резюме

Frontend ожидает REST endpoints для Reader workflow, но в AggregatorService отсутствуют соответствующие HTTP-контроллеры. gRPC-клиенты и методы готовы, но не выставлены наружу через REST API.

## Ожидаемые Frontend Endpoints

| Endpoint | Метод | Назначение | Файл клиента |
|----------|-------|------------|--------------|
| `/api/text/analyze` | POST | Анализ текста, токенизация, подсветка | `text-client.ts` |
| `/api/terms` | POST | Создание/обновление термина (LingQ) | `term-client.ts` |
| `/api/terms/mark-known` | POST | Пометить термин как известный | `term-client.ts` |
| `/api/terms/ignore` | POST | Игнорировать термин | `term-client.ts` |
| `/api/terms/bulk-known` | POST | Массовая пометка при перелистывании | `term-client.ts` |
| `/api/terms/details` | GET | Детали термина (meaning, contexts) | `term-client.ts` |
| `/api/terms/search-duplicates` | POST | Поиск дубликатов термина | `term-client.ts` |
| `/api/Media/library/{projectId}` | GET | Библиотека reader (книги) | `media-client.ts` |
| `/api/Media/upload-document` | POST | Загрузка PDF/документа | `media-client.ts` |
| `/api/Media/serve-document` | GET | Получить файл документа | `media-client.ts` |
| `/api/Media/library/{projectId}/collections` | GET/POST | Коллекции книг | `media-client.ts` |

## Фактическое состояние Aggregator

### Есть контроллеры (8 шт.):
- `AuthController` - аутентификация
- `ProjectsController` - проекты
- `DecksController` - колоды (deck library)
- `CardsController` - карточки
- `StudyController` - сессии обучения
- `AnalyticsController` - аналитика
- `CommunityController` - сообщество
- `UserSettingsController` - настройки пользователя

### Отсутствуют контроллеры (критично):
- ❌ `TextController` - анализ текста
- ❌ `TermsController` - операции с терминами
- ❌ `MediaController` - медиа-библиотека reader

## Готовые gRPC методы (не используются)

В `VocabularyServiceClient.cs` уже реализованы:

```csharp
// TextService
Task<AnalyzeTextResponse> AnalyzeTextAsync(...)

// TermService
Task<TermDetailsResponse> CreateOrUpdateTermAsync(...)
Task<TermDetailsResponse> MarkTermKnownAsync(...)
Task<TermDetailsResponse> IgnoreTermAsync(...)
Task<BulkMarkKnownResponse> BulkMarkKnownAsync(...)
Task<TermDetailsResponse> GetTermDetailsAsync(...)
Task<SearchTermDuplicatesResponse> SearchTermDuplicatesAsync(...)
```

## Критические проблемы

### 1. Отсутствует MediaServiceClientImpl
В `Program.cs` регистрируется:
```csharp
builder.Services.AddSingleton<IMediaServiceClient, MediaServiceClientImpl>();
```

Но файл `MediaServiceClientImpl.cs` **не существует** в репозитории.

### 2. Нет REST прослойки для gRPC
Frontend ожидает REST, но Aggregator не предоставляет HTTP endpoints для:
- Анализа текста
- Операций с терминами
- Медиа-операций

### 3. Несоответствие контрактов импорта
- Frontend: multipart upload с `file + config`, ожидает `ImportJobResponse`
- Aggregator: JSON body `BulkCreateCardsDto`, возвращает список карточек

## Рекомендации

### P0 (критично)
1. Создать `MediaServiceClientImpl` для MediaService gRPC
2. Создать `TextController` с endpoint `/api/text/analyze`
3. Создать `TermsController` со всеми term endpoints
4. Создать `MediaController` для reader library

### P1 (важно)
1. Выровнять контракт импорта карточек (multipart vs JSON)
2. Добавить валидацию и обработку ошибок
3. Добавить rate limiting для text analyze

### P2 (улучшения)
1. Кэширование результатов анализа
2. Batch optimization для bulk operations
3. Observability (метрики, tracing)

## Связанные файлы

- Frontend константы: `polyraspad-frontend/src/lib/constants.ts`
- gRPC клиент: `AggregatorService/Services/VocabularyServiceClient.cs`
- Program.cs: `AggregatorService/Program.cs`
