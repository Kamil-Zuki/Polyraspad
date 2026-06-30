# Введение

REST API Aggregator Service — единственный публичный HTTP-контракт для SPA. Все маршруты под префиксом `/api/` (кроме `/healthz`, `/swagger` в Development).

Аутентификация по умолчанию: **JWT Bearer** (`Authorization: Bearer <access_token>`).

# 1. Группы методов REST API

| Группа | Base route | SR | Auth |
| :--- | :--- | :--- | :--- |
| Auth | `/api/Auth` | SR-AGG-AUTH-* | Public / JWT |
| Projects | `/api/Projects` | SR-AGG-CONTENT-01 | JWT |
| Decks | `/api/Decks` | SR-AGG-CONTENT-02 | JWT |
| Cards | `/api/Cards` | SR-AGG-CARD-* | JWT |
| Study | `/api/study` | SR-AGG-STUDY-* | JWT |
| Analytics | `/api/analytics` | SR-AGG-ANALYTICS-* | JWT |
| Settings | `/api/settings` | SR-AGG-SETTINGS-* | JWT |
| Terms | `/api/terms` | SR-AGG-READER-01 | JWT |
| Text | `/api/text` | SR-AGG-READER-02 | JWT |
| Subscriptions | `/api/subscriptions` | SR-AGG-SUB-* | JWT |
| Community | `/api` | SR-AGG-COMM-* | JWT |
| Media | `/api/Media` | SR-AGG-MEDIA-* | JWT |
| Billing | `/api/Billing` | SR-AGG-BILL-* | JWT / Webhook key |
| Agent | `/api/agent` | SR-AGG-AGENT-* | JWT |
| AI | `/api/ai` | SR-AGG-AI-* | X-Ai-Proxy-Key |
| Automation | `/api/automation` | SR-AGG-AUTO-* | JWT |
| Integrations | `/api/integrations` | SR-AGG-INT-* | JWT |
| Health | `/healthz` | SR-AGG-OPS-01 | Public |

# 2. Общие соглашения

* gRPC errors → HTTP: `InvalidArgument` 400, `NotFound` 404, `Unauthenticated` 401, `PermissionDenied` 403, `AlreadyExists` 409, иначе 502
* Downstream identity: metadata `user_id`, `roles`
* Pagination: cursor-based где поддерживает Vocabulary (terms list)

# 3. Group files (1:1 с группами `01 - Функциональная спецификация`)

| № | Файл | SR |
| :--- | :--- | :--- |
| 01 | [[01 - Аутентификация и профиль (Auth)]] | SR-AGG-AUTH-* |
| 02 | [[02 - Проекты, колоды и настройки (Content)]] | SR-AGG-CONTENT-* |
| 03 | [[03 - Карточки и редактор (Cards)]] | SR-AGG-CARD-* |
| 04 | [[04 - Сессии обучения (Study)]] | SR-AGG-STUDY-* |
| 05 | [[05 - Аналитика (Analytics)]] | SR-AGG-ANALYTICS-* |
| 06 | [[06 - Reader и термины (Reader)]] | SR-AGG-READER-* |
| 07 | [[07 - Подписки на колоды (Deck Subscriptions)]] | SR-AGG-SUB-* |
| 08 | [[08 - Сообщество и маркетплейс (Community)]] | SR-AGG-COMM-* |
| 09 | [[09 - Медиа и Reader Library (Media)]] | SR-AGG-MEDIA-* |
| 10 | [[10 - SaaS-биллинг (Billing)]] | SR-AGG-BILL-* |
| 11 | [[11 - AI-агент (Agent)]] | SR-AGG-AGENT-* |
| 12 | [[12 - AI-прокси (AI Proxy)]] | SR-AGG-AI-01 |
| 13 | [[13 - Автоматизация (Automation)]] | SR-AGG-AUTO-* |
| 14 | [[14 - Внешние интеграции (Integrations)]] | SR-AGG-INT-* |
| 15 | [[15 - Настройки пользователя (Settings)]] | SR-AGG-SETTINGS-* |
| 16 | [[16 - Платформенные контракты (Operations)]] | SR-AGG-OPS-* |

Детальные endpoint blocks — в group files выше.
