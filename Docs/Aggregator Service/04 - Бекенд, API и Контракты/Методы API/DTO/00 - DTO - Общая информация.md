# Введение

DTO Aggregator Service — JSON-контракты REST API. Поля должны соответствовать protobuf messages downstream-сервисов после маппинга. Aggregator не добавляет персистентных полей.

# 1. Группы DTO

| Группа | Файл | SR |
| :--- | :--- | :--- |
| Auth | [[01 - Аутентификация и профиль (Auth)]] | SR-AGG-AUTH-* |
| Content | [[02 - Проекты, колоды и контент (Content)]] | SR-AGG-CONTENT-*, SR-AGG-SUB-* |
| Cards & Study | [[03 - Карточки и обучение (Cards Study)]] | SR-AGG-CARD-*, SR-AGG-STUDY-*, SR-AGG-ANALYTICS-* |
| Reader | [[04 - Reader и термины (Reader)]] | SR-AGG-READER-* |
| Community & Billing | [[05 - Сообщество, биллинг и агент (Community Billing Agent)]] | SR-AGG-COMM-*, SR-AGG-BILL-*, SR-AGG-AGENT-*, SR-AGG-AUTO-* |
| Media & AI | [[06 - Медиа, AI, интеграции и настройки (Media AI Integrations)]] | SR-AGG-MEDIA-*, SR-AGG-AI-01, SR-AGG-INT-*, SR-AGG-SETTINGS-* |

Общие типы: `PaginatedResponseDto<T>` — cursor/page metadata где применимо.

Якоря: `#dto-{TypeName}` в group files.
