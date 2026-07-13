# Polyraspad — Полный реестр функциональных требований

**Версия:** 1.0  
**Дата:** 2026-07  
**Тип:** Продуктовая сводная спецификация (Final Product Vision)  
**Статус:** Living document — дополняется по мере роста сервисов

---

## Введение

Настоящий документ — **сводный реестр всех функциональных требований** платформы Polyraspad, сгруппированных по микросервисам. Он описывает **финальный продукт** — не MVP и не текущее состояние реализации, а полную целевую картину того, чем должна стать платформа.

### Для чего этот документ

Каждый микросервис Polyraspad имеет собственную детальную спецификацию (`01 — Функциональная спецификация/`) со SR-блоками, сценариями и техническими деталями. Этот документ — **единая точка входа**: он даёт продуктовую панораму на одной странице и позволяет понять, какой сервис отвечает за конкретную возможность платформы.

Polyraspad — языковая платформа, построенная вокруг четырёх навыков: **Чтение (R)**, **Аудирование (L)**, **Письмо (W)**, **Говорение (S)**. Каждое функциональное требование в конечном счёте работает на один или несколько из этих навыков.

### Структура документа

Требования сгруппированы по **сервисам** — в том порядке, в котором они стоят в публичном стеке платформы: от пользовательского интерфейса вниз к инфраструктурным сервисам.

| № | Сервис | Роль | SR-префикс |
| :--- | :--- | :--- | :--- |
| 1 | **Aggregator Service** | Публичный REST BFF / API Gateway | `SR-AGG-*` |
| 2 | **Vocabulary Service** | Доменный движок: карточки, обучение, ридер, контент | `SR-VOC-*` |
| 3 | **Agent Service** | ИИ-ассистент PolyGuide: диалоги, обучающие инструменты | `SR-AGENT-*` |
| 4 | **Authorization Module** | Identity: регистрация, JWT, email-верификация | `SR-AUTHMOD-*` |
| 5 | **Media Service** | Объектное хранилище: медиа, TTS, Reader Library | `SR-MEDIA-*` |
| 6 | **Billing Service** | SaaS-биллинг: тарифы, подписки, entitlements, invoices | `SR-BILL-*` |
| 7 | **inclusive** | Python: FSRS scheduling, NLP токенизация/лемматизация | `SR-INC-*` |

> **Детальные SR-блоки** (цели, принципы, сценарии) находятся в файлах каждого сервиса в `Docs/<Service>/01 - Функциональная спецификация/`.

---

## Содержание

### 1. Aggregator Service — REST BFF / API Gateway

*Тонкий шлюз между клиентом (браузер, Next.js) и внутренними микросервисами. Принимает HTTP, валидирует JWT локально, проксирует gRPC-вызовы во внутренние сервисы.*

> Детальная спецификация: [[Aggregator Service/01 - Функциональная спецификация/Возможности сервиса/00 - Общая информация|Aggregator — Общая информация]]

| Группа | Название и Описание |
| :----- | :--- |
| **Аутентификация и профиль (Auth Proxy)** | REST-фасад register, login, refresh, logout, profile и rate limit; credentials не хранятся на BFF. |
| **Контент: проекты и колоды (Content)** | CRUD project/deck, дерево библиотеки и передача identity в ContentService. |
| **Карточки и редактор (Cards)** | CRUD note/card, поиск и import; динамическая схема note type для Card Editor. |
| **Сессии обучения (Study)** | Запуск FSRS-сессии и интерактивный цикл next/review/undo через StudyService. |
| **Аналитика (Analytics)** | Dashboard-аналитика: статистика словаря, heatmap, daily summary с graceful degradation. |
| **Reader и термины (Reader)** | Term-first операции: save, mark-known, ignore, bulk; анализ текста для подсветки страницы. |
| **Подписки на колоды (Deck Subscriptions)** | Список, оформление и отмена follow на published/shared decks. |
| **Сообщество и маркетплейс (Community)** | Contributions, publish/fork, products и проверка deck entitlement перед premium access. |
| **Медиа и Reader Library (Media)** | Upload image/document, TTS, извлечение текста, CORS-proxy и Reader library CRUD. |
| **SaaS-биллинг (Billing)** | Self-service billing: access, entitlements, subscription, checkout, cancel, invoices; webhooks. |
| **AI-агент (Agent)** | Жизненный цикл AI-тредов: список, создание, сообщения, run и archive. |
| **AI-прокси (AI Proxy)** | LLM-прокси для Next.js BFF: models, generate и mining-draft по shared secret. |
| **Автоматизация (Automation)** | Copilot feedback и A/B experiments (частично stub в текущей реализации). |
| **Внешние интеграции (Integrations)** | Outbound HTTP к MyMemory и Free Dictionary; lookup по exact word form. |
| **Настройки пользователя (Settings)** | Reader preferences и study defaults: чтение и обновление per user. |
| **Платформенные контракты (Operations)** | Health, CORS, fail-fast Production config и Swagger (dev). |
| **Уроки и прогресс (Lessons)** | Список и детали уроков, запуск сессий с ИИ-агентом, перезапуск и фиксация результатов. |
| **Автопилот дня (Autopilot)** | Получение daily-плана занятий на основе рекомендаций downstream-аналитики. |

---

### 2. Vocabulary Service — Доменный движок

*Ключевой domain-сервис платформы. Хранит всё, что связано с учёбой: проекты, колоды, карточки, термины, сессии, аналитику, контент маркетплейса и учебный план (Curriculum).*

> Детальная спецификация: [[Vocabulary Service/01 - Функциональная спецификация|Vocabulary Service — Спецификация]]

| Группа | Название и Описание |
| :----- | :--- |
| **Управление контентом (ContentService)** | CRUD проектов и колод, дерево библиотеки, права на уровне project/deck. |
| **Карточки и редактор (CardService)** | CRUD note/card, note types, динамические поля, import из CSV и duplicate-check. |
| **Система интервального повторения (StudyService + FSRS)** | Формирование study queue, FSRS-оценки через `inclusive`, leech-detection, undo. |
| **Reader и термины (TermService + TextService)** | Term-first модель: сохранение статусов exact form, bulk-known, анализ текста (токены, фразы, статусы). |
| **Аналитика и Автопилот (AnalyticsService)** | Dashboard stats, heatmap, daily summary, Radar Chart навыков (R/L/W/S), daily autopilot plan. |
| **Сообщество и маркетплейс (CommunityService)** | Contributions, publish, fork, author profiles, product catalog, deck entitlements. |
| **Подписки на колоды (SubscriptionService)** | Follow/unfollow shared decks; sync subscribed deck cards. |
| **Учебный план и прогресс (LessonService)** | Curriculum map (A1→C2), lesson lifecycle, UserLessonProgress, UserCefrProgress, Placement Test. |
| **AI-хелперы (AIService)** | ExplainGrammar, GenerateContext, mining drafts через LLM — только backend вызовы. |
| **Оценка навыков (EvaluationService)** | SkillAssessment, TestResult: сбор результатов диктантов, ролевых игр и writing challenges. |
| **Шэдоуинг (ShadowingService)** | Хранение попыток shadowing: TTS audio, user recording URL, self-rating per sentence. |
| **Платформенные контракты (Operations)** | Health, EF migrations, gRPC-only Kestrel, Redis кэш. |

---

### 3. Agent Service — ИИ-ассистент PolyGuide

*AI-ассистент PolyGuide: хранит треды диалога, историю сообщений и runs. Основной поток — ExecuteRun: domain classify → intent route → learning tool → persist.*

> Детальная спецификация: [[Agent Service/01 - Функциональная спецификация/Возможности сервиса/00 - Общая информация|Agent Service — Общая информация]]

| Группа | Название и Описание |
| :----- | :--- |
| **Управление тредами (Thread Management)** | Список, создание, получение и архивация AI-тредов в контексте project. |
| **История сообщений (Message History)** | Cursor-пагинация user/assistant сообщений треда. |
| **Запуски агента (Agent Runs)** | CreateRun (persist) и ExecuteRun (server orchestration): domain → intent → tool → persist. |
| **Доменная политика (Domain Policy)** | Domain gate: классификация in/out of scope до вызова LLM-tools; refusal copy. |
| **Маршрутизация намерений (Intent Routing)** | Regex-based выбор tool по тексту пользователя; приоритетная цепочка. |
| **Инструменты обучения (Learning Tools)** | explain_word, grammar_help, generate_example, build_card_draft, general_answer, writing_check. |
| **Голосовое взаимодействие (Voice Tools)** | Voice mode: TTS озвучка ответов агента, STT ввод пользователя через Push-to-Talk. |
| **Проактивный агент (Proactive Agent)** | Scheduled push уведомлений: Agent инициирует напоминания и предлагает персонализированные сессии. |
| **Навигация и прогресс (Navigation & Progress)** | navigate (Reader/Study/…) и get_progress (stats, Radar Chart). |
| **Динамическая доска (Dynamic Whiteboard)** | UI-пуш виджетов агентом: таблицы, fill-in-the-blank, диаграммы на доске урока. |
| **Артефакты (Artifacts)** | Create и list structured JSON payloads, привязанных к run+thread. |
| **Интеграция с Vocabulary (Vocabulary Integration)** | Project access, analytics, AI mining helpers через gRPC. |
| **LLM-провайдер (LLM Provider)** | OpenAI-compatible chat completion; model/timeout из options. |
| **Платформенные контракты (Operations)** | Health, EF migrations, gRPC-only Kestrel. |

---

### 4. Authorization Module — Identity и аутентификация

*Микросервис identity: учётные записи, JWT access/refresh tokens, email-верификация, gRPC API для Aggregator.*

> Детальная спецификация: [[Authorization Module/01 - Функциональная спецификация|Authorization Module — Спецификация]]

| Группа | Название и Описание |
| :----- | :--- |
| **Регистрация и учётные записи (Registration)** | Создание учётной записи: email/password, хэширование, валидация уникальности. |
| **Аутентификация и выдача JWT (Authentication)** | Login, выдача пары access/refresh token, проверка пароля через ASP.NET Core Identity. |
| **Обновление токена (Token Refresh)** | Продление сессии по refresh token без повторного ввода пароля. |
| **Email-верификация (Email Confirmation)** | Одноразовая ссылка подтверждения через SMTP; защита от нескольких отправок. |
| **Управление профилем (Profile)** | Чтение и обновление атрибутов пользователя: username, password, avatar. |
| **Выход и отзыв токена (Logout)** | Завершение сессии и инвалидация refresh token в PostgreSQL. |
| **Rate Limiting (PublicAuth)** | Защита публичных endpoints register, login, refresh, confirm-email. |
| **Платформенные контракты (Operations)** | Health, EF migrations, gRPC-only server, Swagger (dev). |

---

### 5. Media Service — Объектное хранилище

*Внутренний gRPC-сервис для работы с бинарными медиа в MinIO (bucket `polyraspad-media`) и метаданными Reader Library.*

> Детальная спецификация: [[Media Service/01 - Функциональная спецификация|Media Service — Спецификация]]

| Группа | Название и Описание |
| :----- | :--- |
| **Загрузка медиа (Upload)** | Upload изображений и документов (PDF, EPUB, TXT) в MinIO; multipart, content-type validation. |
| **Получение URL медиа (Resolve URL)** | Pre-signed URL или proxy URL для безопасной раздачи файлов без CORS-блокировки. |
| **Reader Library CRUD** | Создание, получение, обновление (lastReadPage), удаление записей библиотеки; share/unshare collaborators. |
| **Синхронизация прогресса чтения** | Сохранение позиции чтения (pageIndex, pageNumber) и дата последнего обращения. |
| **Коллекции библиотеки (Collections)** | Группировка книг в коллекции; shared collections между пользователями. |
| **Платформенные контракты (Operations)** | Health, gRPC-only Kestrel, MinIO connectivity check. |

---

### 6. Billing Service — SaaS-биллинг

*Provider-agnostic SaaS-биллинг: тарифные планы, подписки, entitlements, инвойсы и webhook-оркестрация.*

> Детальная спецификация: [[Billing Service/01 - Функциональная спецификация|Billing Service — Спецификация]]

| Группа | Название и Описание |
| :----- | :--- |
| **Каталог тарифных планов (Plans)** | CRUD тарифов (`free`, `pro`, …); feature flags и limits per plan. |
| **Управление подпиской (Subscription)** | Создание, обновление, отмена и reactivation подписок пользователя. |
| **Entitlements и проверка доступа** | Проверка активного entitlement перед началом premium-операции; grace period. |
| **Checkout и платёжные сессии** | Создание checkout session через payment provider (YooKassa / mock); redirect flow. |
| **Инвойсы и история платежей** | Список инвойсов и статусов платежей для пользователя. |
| **Входящие payment webhooks** | Получение событий от провайдера (paid, failed, refunded); idempotency ключ. |
| **Платформенные контракты (Operations)** | Health, EF migrations, gRPC-only Kestrel. |

---

### 7. inclusive — Python NLP + FSRS microservice

*Python gRPC микросервис: математические расчёты FSRS (Free Spaced Repetition Scheduler) и NLP-обработка текста (токенизация, лемматизация, POS-tagging).*

> Детальная спецификация: [[inclusive/README|inclusive — README]] и `inclusive/proto/vocab.proto`.

| Группа | Название и Описание |
| :----- | :--- |
| **FSRS Review (Scheduling)** | Принимает текущий FsrsState карточки и оценку (Again/Hard/Good/Easy); возвращает новый state, дату Due, Stability, Difficulty. |
| **Текстовая токенизация (Tokenization)** | Разбиение текста на токены с NLTK; позиция (offsets) каждого токена. |
| **Лемматизация и POS-tagging** | Приведение формы к лемме и определение части речи для точного term-first matching. |
| **Платформенные контракты (Operations)** | gRPC health; `config.json` для порта; pytest контрактные тесты. |
