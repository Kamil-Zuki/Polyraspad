# Фича: Словарь, Дерево Колод и Редактор Заметок (Vocabulary, Decks & Editor)

**Статус:** Implemented  
**Связанный бекенд:** Vocabulary Service (`ContentService.GetDeckTree`, `CardService.CreateCard`, `TermService.ListProjectTerms`, `NoteTypeService`).

---

## 1. UX-сценарий (User Journey)

* **Шаг 1: Управление колодами (`/decks`).** Пользователь просматривает иерархическое дерево колод (Deck Tree). Фильтрует колоды ("Мои", "Скачанные", "Публичные").
* **Шаг 2: Список терминов (`/vocabulary`).** Пользователь просматривает полный список слов и фраз проекта (`ListProjectTerms`). Фильтрует по статусам (`SAVED`, `KNOWN`, `IGNORED`). Осуществляет пакетные операции (Bulk Mark Known).
* **Шаг 3: Редактор заметок (`/editor`).** Пользователь создает или редактирует тип заметки (NoteType в стиле Anki). Настраивает поля (`NoteFieldDefinitionPayload`) и шаблоны карточек (`CardTemplatePayload`).

---

## 2. Маршрутизация и Страницы (Routing)

* `src/app/decks/page.tsx` — дерево колод и детали управления.
* `src/app/vocabulary/page.tsx` — реестр терминов проекта.
* `src/app/editor/page.tsx` — конструктор заметок и шаблонов.

---

## 3. Дерево компонентов (Component Architecture)

```
<DecksPage> (Client)
├── <LibraryFilterTabs> — вкладки ("Mine", "Downloaded", "Public")
├── <DeckTreeView> — иерархическое дерево колод с раскрывающимися ветками
│   └── <DeckNode> — узел колоды с обложкой, счетчиками New/Due/Total и действиями
└── <DeckDetailDrawer> — боковая панель с деталями и статистикой колоды

<VocabularyPage> (Client)
├── <TermFilterHeader> — поиск по фразам, выбор статуса (SAVED/KNOWN/IGNORED)
├── <TermDataTable> — виртуализированная таблица терминов
└── <BulkActionBar> — плавающая панель для массовой пометки выученными
```

---

## 4. Интеграция с API (Data Fetching & BFF)

* **Чтение (Queries):**
  * `GET /api/v1/decks/tree` (`ContentService.GetDeckTree`) — дерево колод.
  * `GET /api/v1/terms` (`TermService.ListProjectTerms`) — список терминов с пагинацией по курсору.
  * `GET /api/v1/editor/notetype` (`CardService.GetNoteTypeForEditor`) — схема типов заметок.
* **Мутации (Mutations):**
  * `POST /api/v1/decks` (`ContentService.CreateDeck`) — создание колоды.
  * `PUT /api/v1/decks/{id}` (`ContentService.UpdateDeck`) — редактирование колоды.
  * `DELETE /api/v1/decks/{id}` (`ContentService.DeleteDeck`) — удаление колоды.

---

## 5. Управление состоянием (State Management)

* **Локальное состояние (`EditorContext`):**
  * `activeNoteType`: редактируемый тип заметки в конструкторе.
  * `selectedTermIds`: выбранные строки в таблице терминов для массовой пометки.
* **Кэш React Query:**
  * `['decks', 'tree', projectId]` — кэш дерева колод. Инвалидируется при создании/удалении колоды.

---

## 6. Стратегия тестирования фронтенда (UI Testing)

* **Компонентные тесты (`src/components/decks/decks.test.tsx`):**
  * Проверка корректного построения рекурсивного дерева колод.
  * Проверка переключения фильтра библиотеки (Mine / Downloaded / Public).
  * Проверка работы чекбоксов массовой пометки в VocabularyDataTable.
