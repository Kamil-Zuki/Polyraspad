# Введение

**Aggregator Service не владеет персистентной моделью данных.** Папка `03` описывает **контракты на границе API Gateway** — логические представления JSON DTO, с которыми работает frontend. Источник истины для доменных сущностей — downstream-сервисы.

## Принцип

| Аспект | На BFF | Downstream |
| :--- | :--- | :--- |
| PostgreSQL tables | Нет | VocabularyService, authorization-module, BillingService, … |
| In-memory jobs | Да (`Automation` jobs) | — |
| JSON request/response shapes | Да (DTO) | gRPC messages |
| JWT claims context | Да (transient) | — |

## Группы контрактных сущностей

| Группа | Файл | Downstream |
| :--- | :--- | :--- |
| Auth API | [[Entity - Аутентификация и профиль - Auth Proxy]] | authorization-module |
| Content | [[Entity - Проекты и колоды - Content]] | VocabularyService Content/Deck |
| Cards & Study | [[Entity - Карточки и обучение - Cards Study]] | CardService, StudyService, Analytics |
| Reader | [[Entity - Reader и термины - Reader]] | TermService, TextService |
| Media | [[Entity - Медиа и библиотека - Media]] | MediaService |
| Community & Billing | [[Entity - Сообщество и биллинг - Community Billing]] | Community, Subscriptions, Billing |
| Lessons & Autopilot | [[Entity - Уроки и автопилот - Lessons Autopilot]] | Lesson + Autopilot (+ feature flags) |

Подробные поля DTO — в коде `AggregatorService/Dtos/` и в `04 - Бекенд, API и Контракты/Методы API/DTO/` (если заполнено).
