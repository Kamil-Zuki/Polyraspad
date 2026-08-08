# Changelog (История изменений)

Все заметные изменения в платформе Polyraspad (микросервисы, frontend, агенты) документируются в этом файле.
Формат основан на стандарте [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/).

## [Unreleased]

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
