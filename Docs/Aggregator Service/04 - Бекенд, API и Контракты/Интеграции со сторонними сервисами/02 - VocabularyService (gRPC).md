# Введение

Основной downstream домен Polyraspad. Proto: `vocabulary.proto`. Config: `AggregatorService:VocabularyServiceBaseUrl`.

# Общая информация

| Параметр | Значение |
| :--- | :--- |
| **Сервисы в proto** | Content, Card, Study, Term, Text, Analytics, Community, Subscription, Settings |
| **SR** | SR-AGG-CONTENT-* … SR-AGG-SETTINGS-* (кроме Auth/Media/Billing/Agent) |

# Маппинг REST → gRPC (сводка)

| Группа REST | gRPC service |
| :--- | :--- |
| Projects / Decks | ContentService |
| Cards | CardService |
| Study | StudyService |
| Terms / Text | TermService, TextService |
| Analytics | AnalyticsService |
| Subscriptions / Community | SubscriptionService, CommunityService |
| Settings | UserSettingsService (или эквивалент в proto) |

# Metadata

На authenticated calls BFF добавляет:

```
user_id: <sub from JWT>
roles: <comma-separated roles>
```

# Term-first контракт

Term DTO маппятся без lemma-id. Duplicate detection — normalized exact text на стороне VocabularyService.

# Graceful degradation

`GetDailySummary` — при недоступности Analytics BFF возвращает default DTO (см. REST [[05 - Аналитика (Analytics)]]).
