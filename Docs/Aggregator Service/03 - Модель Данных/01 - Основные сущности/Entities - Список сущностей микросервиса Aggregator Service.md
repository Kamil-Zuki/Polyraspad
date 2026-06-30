# Введение

**Aggregator Service не владеет персистентной моделью данных.** Папка `03` описывает **контракты на границе API Gateway** — логические представления JSON DTO, с которыми работает frontend. Источник истины для сущностей — downstream-сервисы (см. их документацию).

## Принцип

| Аспект | На BFF | Downstream |
| :--- | :--- | :--- |
| PostgreSQL tables | Нет | VocabularyService, authorization-module, … |
| Redis / RabbitMQ | Нет | Auth, Vocabulary (по сервису) |
| JSON request/response shapes | Да (DTO) | gRPC messages |
| JWT claims context | Да (transient) | — |

## Группы контрактных сущностей

| Группа | Файл | Downstream |
| :--- | :--- | :--- |
| Auth API | [[Entity - Аутентификация и профиль - Auth Proxy]] | authorization-module |
| Content | [[Entity - Проекты и колоды - Content]] | VocabularyService.ContentService |
| Cards & Study | [[Entity - Карточки и обучение - Cards Study]] | CardService, StudyService |
| Reader | [[Entity - Reader и термины - Reader]] | TermService, TextService |
| Media | [[Entity - Медиа и библиотека - Media]] | MediaService |
| Community & Billing | [[Entity - Сообщество и биллинг - Community Billing]] | CommunityService, BillingService |

Подробные поля DTO — в `04 - Бекенд, API и Контракты/Методы API/DTO/`.
