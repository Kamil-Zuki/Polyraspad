# Changelog (История изменений)

Все заметные изменения в платформе Polyraspad (микросервисы, frontend, агенты) документируются в этом файле.
Формат основан на стандарте [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/).

## [Unreleased]

## [0.7.0] - 2026-08-10

### Added (Добавлено)
- **Vocabulary (Frontend):** Добавлено отображение общего количества карточек и терминов при пагинации в интерфейсе Word Bank и SRS Cards.
- **Vocabulary (Frontend):** Кликабельный Distribution bar на странице Vocabulary и Dashboard — при клике на сегмент (например, "Known") происходит переход в Word Bank с автоматическим применением фильтра.

### Changed (Изменено)
- **Vocabulary (Backend/Frontend):** Миграция с курсорной пагинации (`cursor`, `nextCursor`) на смещение (`pageNumber`, `totalCount`) для списка терминов проекта (`ListProjectTerms`).
- **Vocabulary (Frontend):** Переработан стиль списка фильтров `Quick Lists` во вкладке `SRS Cards` (переведены в единый блок Toggle Group для улучшения интерфейса).
- **Vocabulary (Frontend):** Глубокий рефакторинг `cards-tab.tsx` (разделение на `CardsTable`, `CardsToolbar`) и вынос логики иерархии колод в общий хук `useFlatDecks(projectId)`.
- **Vocabulary (Frontend):** Названия вкладок заменены: `Terms` -> `Word Bank`, `Cards` -> `SRS Cards`. Добавлены счетчики элементов в переключатель вкладок.
- **Vocabulary (Frontend):** Таблица Terms: объединены колонки Term и Meaning, скрыт Type, контекст теперь сворачивается, а Meaning можно редактировать прямо в таблице. Добавлены чекбоксы для массовых операций.

## [0.6.0] - 2026-08-10

### Added (Добавлено)
- **Project Deletion (Full Cascade & Media Cleanup):** Добавлена возможность полного безвозвратного удаления языковых проектов.
  - **Backend (`VocabularyService`, `MediaService`, `AggregatorService`):** Добавлены gRPC методы `DeleteProject` и `DeleteProjectMedia`, каскадное удаление данных в PostgreSQL (колоды, карточки, слова, термины, леммы, FSRS прогресс, история), запись надгробного камня `DeletedObject` (tombstone) и полная очистка объектов MinIO S3 (индексы книг Ридера, коллекции, выжимки выдержек `extracted.json`). В `AggregatorService` добавлен REST эндпоинт `DELETE /api/Projects/{id}`.
  - **Frontend (`polyraspad-frontend`):** Реализован `useDeleteProject` мутационный хук, опасный диалог подтверждения `DeleteProjectDialog` с валидацией ввода имени проекта и кнопки удаления на карточках и в настройках проекта.

## [0.5.0] - 2026-08-09

### Added (Добавлено)
- **Text Workspace (Frontend):** Добавлен редактор текстовых книг (`/library/editor`), поддерживающий написание собственного текста, прикрепление обложки книги и авто-генерацию TTS-аудио. Введенный текст автоматически сохраняется как `.txt` файл в библиотеке.
- **Library (Frontend):** Обновлен дизайн карточек книг (`ReaderLibraryBookCard`): теперь они поддерживают отображение пользовательских обложек на фоне, а также бейджи уровня сложности CEFR и наличия аудио (`AUDIO`).
- **Billing & Limits (Backend):** Внедрены ограничения (Freemium Limits) на создание пользовательских текстовых книг. На тарифе `Free` установлено ограничение в 3 текстовые книги (`textWorkspaceMaxBooks`), для `Pro` лимиты отключены. Интегрировано в `BillingService` и `MediaController`.
## [0.4.0] - 2026-08-08

### Changed (Изменено)
- **Reader (Frontend):** Уменьшен размер шрифта заголовков в режиме чтения (в `reader-reading-article.tsx`) для более органичного вида (текст больше не разрывается гигантскими надписями).
- **Reader (Frontend):** Смягчена эвристика определения заголовков (в `reader-utils.ts`), чтобы обычные предложения, начинающиеся с заглавной буквы (например, с имени), не распознавались ошибочно как заголовки.
### Added (Добавлено)
- **Profile (Frontend):** Добавлена новая секция **«Billing & Subscription»** (`ProfileBillingSection`) в Профиль (`/profile`), позволяющая просматривать текущий тариф, лимиты проектов/агетов/карточек и быстро переходить к управлению подпиской.
- **Sidebar (Frontend):** В карточку пользователя в сайдбаре добавлен бейдж подписки (`SubscriptionBadge`), показывающий текущий тариф.

### Changed (Изменено)
- **Billing (Frontend):** Полностью переработан дизайн страниц `/billing` и `/billing/invoices` под визуальный стандарт **Profile Studio** (стеклянные карточки `glass-panel`, закгругления `rounded-[2rem]`, фоновые градиенты, карточки тарифов с акцентными кнопками и сетка лимитов).

## [0.3.0] - 2026-08-08

### Added (Добавлено)
- **Library (Frontend):** Добавлено модальное диалоговое окно подтверждения удаления книги из библиотеки (`DeleteBookDialog`) с предупреждением и кнопками отмены/удаления.

### Changed (Изменено)
- **Library (Frontend):** Поменяли местами кнопки на карточках книг: кнопка **«Read» / «Читать»** теперь первой (слева), а кнопка **Удаления** — второй (справа) для единообразия в виде списка и сетки.

### Fixed (Исправлено)
- **Projects (Frontend):** Исправлено выпадающее меню выбора изучаемого (`Target`) и исходного (`Source`) языка в диалоге создания нового проекта ([create-project-dialog.tsx](file:///c:/Users/Zuko/Desktop/01Projects/Development_Documents/Polyraspad/polyraspad-frontend/src/components/projects/create-project-dialog.tsx)). Список языков динамически сопоставлен с `STUDY_LANGUAGE_PRESETS`, благодаря чему стали доступны **Корейский (Korean / `ko`)**, **Японский**, **Французский**, **Китайский** и другие языки.

## [0.2.0] - 2026-08-08

### Added (Добавлено)
- **Reader (Frontend):** Добавлен режим и кнопка-тумблер **«Книжный стиль»** (`Book Style`) в шапке ридера. Реализовано академическое книжное форматирование интерактивной текстовой панели: шрифт Serif (Georgia/Cambria), выравнивание по ширине (`text-align: justify`), красная строка для абзацев (`text-indent: 1.5em`) и сохранение состояния в `localStorage`.

### Fixed (Исправлено)
- **Reader (Frontend):** Исправлен контраст и читаемость текста на светлых темах читателя (**Paper** и **Sepia**). Цвет текста интерактивного транскрипта PDF и EPUB адаптирован под тёмные и светлые темы (глубокие чернила вместо блеклого серого `text-gray-300`). Активные выделения слов/фраз, кнопки вкладок (`Split/Page/Text`), плашка `tokens` и тумблер `Book Style` переведены на сочные высококонтрастные оттенки для комфортного чтения при любом освещении.
- **Frontend Build (Docker):** Синхронизирован `package-lock.json` с добавлением отсутствовавшим пакета `@swc/helpers@0.5.23`, что устранило ошибку `npm ci` при сборке Docker-образа `polyraspad-frontend`.
- **Frontend (Reader & Vocabulary):** Исправлено сохранение и отображение внутренних URL хранилища MinIO (`http://minio:9000/polyraspad-media/documents/...`) в поле `sourceUrl` при майнинге слов/фраз из книг. Теперь вместо внутренних бинарных ссылок MinIO генерируется корректный пользовательский URL ридера (`/reader?bookId={id}`), а также выполняется автоматическая очистка старых системных ссылок хранилища.
- **Frontend:** Удалена кодировка UTF-8 BOM из файла `polyraspad-frontend/src/app/auth/confirm/page.tsx`, приводившая к ошибке сборки Turbopack в Next.js 16 (`failed to convert rope into string`).


## [0.1.0] - 2026-07-18 - MVP (Этап 1)

### Added (Добавлено)
- **Billing:** Интеграция провайдера ЮKassa в `BillingService` и `AggregatorService` для приема платежей в РФ, реализована страница выбора тарифа `/billing`.
- **Extension:** Настроен эндпоинт `CaptureCard` для сбора фраз и субтитров из расширения Chrome с проверкой на точные дубликаты.

### Changed (Изменено)
- **Feature Flags:** Скрыт продвинутый функционал AI-агентов (Inspector, Assistant, Auto-drafts, AI Audio) и сложные модули (Lessons, Marketplace, Shadowing) под флаги `NEXT_PUBLIC_FF_AI_AGENTS` и `NEXT_PUBLIC_FF_ADVANCED_MODULES` для MVP релиза.
- **Reader & FSRS:** Финализирован базовый цикл LingQ -> FSRS. Усилена консистентность статусов карточек (SAVED, KNOWN), обеспечено разделение форм слов (напр. sleep/slept) без агрессивной лемматизации.
