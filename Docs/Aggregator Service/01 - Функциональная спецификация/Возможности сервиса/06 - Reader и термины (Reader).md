# Группа 6: Reader и термины (Reader)

## Введение

В этом разделе описывается REST-прокси к **VocabularyService.TermService** и **TextService** — **LingQ-style Reader**: подсветка слов по term-first модели, сохранение жёлтых терминов (SAVED), known/ignore, bulk-known при page turn, анализ текста страницы.

Тerm-first правило: **точная форма** (`sleep` ≠ `slept`) — отдельный `ProjectTerm`; дубликаты по `NormalizedText` (trim + lowercase). Aggregator не лемматизирует и не объединяет формы — только проксирует gRPC.

**Метафора:**

Представьте **читальный зал с цветными маркерами**. Вы читаете текст (Reader UI); библиотекарь (Aggregator) не решает, знаете ли вы слово — он спрашивает **картотеку терминов** (TermService/TextService) и возвращает цвет каждого слова. Когда вы кликаете слово, заявка «сохранить жёлтым» снова идёт через библиотекаря.

REST-контракты: [[04 - Бекенд, API и Контракты/Методы API/REST API/06 - Reader и термины (Reader)|REST API — Reader]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Reader и term-first API.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-READER-01** | **Управление терминами Reader (term-first):** Сохранение, mark-known, ignore и bulk-known — статусы по exact form через TermService. |
| **SR-AGG-READER-02** | **Подсветка текста в Reader:** Токенизация и статусы для страницы; фраза имеет приоритет над отдельными словами; лимит 100 000 символов. |
| **SR-AGG-READER-03** | **Инспектор термина и служебные операции:** Детали термина, поиск exact-дубликатов и очистка demo-import в project. |

---

# Детальная спецификация требований

## SR-AGG-READER-01: Операции с терминами (term-first) {#SR-AGG-READER-01}

Reader UI и vocabulary inspector управляют **ProjectTerm** + **UserTermStatus** через REST. Статусы: NEW (синий), SAVED (жёлтый), KNOWN (белый), IGNORED (приглушённый).

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Term-first** | Разные формы — разные термины; не лемматизация. |
| **Thin BFF** | `TermGrpcMapper` — mapping only; логика статусов в TermService. |
| **Cursor pagination** | GET list: cursor по `UserTermStatus.UpdatedAt DESC, TermId ASC`. |
| **JWT** | Все routes под `[Authorize]`. |
| **Phrase type** | PHRASE terms — отдельные ProjectTerm; phrase highlight priority в analyze. |
| **Duplicate check** | По `NormalizedText` (trim + lowercase), не по lemma. |
| **502 mapping** | gRPC errors → 400/403/404/502 via `MapRpc`. |

### 2. Высокоуровневое описание

Представим термины как **карточки в картотеке Reader**.

1. **Список (ListProjectTerms):** UI vocabulary page запрашивает страницу терминов с фильтрами status/type/search query.
2. **Сохранение (CreateOrUpdateTerm):** клик по синему слову → жёлтый SAVED с meaning и context — POST `/api/terms`.
3. **Known / Ignore:** быстрые действия без карточки SRS — POST mark-known или ignore.
4. **Bulk known:** при page turn (если включена настройка) — POST bulk-known со списком term ids.
5. **Aggregator:** JWT → metadata; **не** подменяет userId из body.

Таким образом, Reader остаётся **term-first** end-to-end: Aggregator не схлопывает `went` и `go` в один статус.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Инициатор:** Reader UI, Vocabulary page.
* **Base:** `/api/terms`
* **Downstream:** `ListProjectTerms`, `CreateOrUpdateTerm`, `MarkTermKnown`, `IgnoreTerm`, `BulkMarkKnown`
* **JWT:** Bearer обязателен.

#### Сценарий А: Сохранение слова как SAVED (Happy Path)

**Сценарий:** Пользователь кликает синее слово в Reader.

1. **POST** `/api/terms` с text, projectId, meaning.
2. **gRPC:** `CreateOrUpdateTerm`.
3. **Ответ:** HTTP **201**, `TermDetailsDto`, status SAVED.

#### Сценарий Б: Mark known (Happy Path)

1. **POST** `/api/terms/mark-known` с `TermActionDto`.
2. **gRPC:** `MarkTermKnown` → HTTP **200**.

#### Сценарий В: Bulk known при page turn (Happy Path)

**Сценарий:** Включена настройка [[15 - Настройки пользователя (Settings)|Settings]] — mark blue as known on page turn.

1. **POST** `/api/terms/bulk-known` с массивом term ids синих слов страницы.
2. **gRPC:** `BulkMarkKnown`.
3. **Ответ:** HTTP **200**, `BulkMarkKnownResponseDto.updatedCount`.

#### Сценарий Г: sleep и slept — разные термины (Happy Path)

**Сценарий:** Регрессия term-first — разные формы не схлопываются.

1. **POST** mark-known для `sleep` → status KNOWN для term id A.
2. **POST** analyze или list — `slept` остаётся NEW (term id B).
3. **Domain:** A ≠ B по `NormalizedText`.

#### Сценарий Д: Missing projectId on list (Negative Path)

1. **GET** `/api/terms` без projectId.
2. **BFF:** HTTP **400** `{ "error": "projectId is required" }`.

---

## SR-AGG-READER-02: Подсветка текста в Reader {#SR-AGG-READER-02}

Перед отображением страницы Reader запрашивает **токенизацию + статусы** для подсветки. Фразы имеют приоритет над отдельными словами (phrase highlight).

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Max length** | 100 000 символов — validation на BFF до gRPC. |
| **TextService** | `AnalyzeText` в VocabularyService. |
| **Mapper** | `ReaderTextMapper.ToHttpResponse` — tokens, phrases, stats. |
| **Controller** | `TextController`, base `/api/text`. |
| **Phrase priority** | Phrase spans override individual word highlights in UI. |

### 2. Высокоуровневое описание

Представим analyze как **сканирование страницы подсветкой**.

1. **Frontend:** отправляет полный текст главы + projectId (после загрузки EPUB/HTML chunk).
2. **Aggregator:** проверяет длину; если OK — gRPC `AnalyzeText`.
3. **Vocabulary:** сопоставляет токены с ProjectTerms пользователя, возвращает offsets и statuses.
4. **UI:** красит NEW/SAVED/KNOWN/IGNORED; phrase spans перекрывают word spans.

Текст длиннее лимита отклоняется на BFF **без** нагрузки на NLP downstream.

Таким образом, analyze остаётся **read-only projection** term statuses на текст страницы; BFF защищает downstream от oversized payload.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** POST `/api/text/analyze`
* **Body:** `TextAnalyzeRequestDto` (projectId, text)

#### Сценарий А: Подсветка страницы (Happy Path)

1. **POST** analyze с text ≤ 100k.
2. **gRPC:** `AnalyzeText`.
3. **Ответ:** HTTP **200**, `TextAnalyzeResponseDto` (tokens, phrases, stats).

#### Сценарий Б: Text too long (Negative Path)

**Сценарий:** Frontend отправляет целую книгу одним chunk.

1. **POST** с text.length > 100 000.
2. **BFF validation:** HTTP **400** `{ "error": "InvalidRequest", "message": "Text is too long …" }`.
3. **gRPC:** не вызывается.

#### Сценарий В: Missing projectId (Negative Path)

1. **POST** analyze без projectId или text.
2. **BFF:** HTTP **400**.

---

## SR-AGG-READER-03: Инспектор термина и служебные операции {#SR-AGG-READER-03}

Вспомогательные операции для inspector, dedup UX и cleanup demo import.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Details** | GET `/api/terms/details?projectId=&termText=&type=` |
| **Duplicates** | POST `search-duplicates` перед create phrase/word |
| **Purge demo** | POST `purge-demo-import?projectId=` — удаление demo automation import |

### 2. Высокоуровневое описание

Представим это как **уточняющие запросы к картотеке**.

1. **Details:** inspector открывает слово — нужен полный `TermDetailsDto` (meaning, first sentence, status).
2. **Search duplicates:** editor проверяет, есть ли уже такая **точная** форма или фраза в project.
3. **Purge demo:** dev/demo flow удаляет карточки и term rows от demo import — опасная операция, только для project owner по правилам Vocabulary.

Aggregator передаёт query/body as-is после JWT check.

Таким образом, inspector и dedup UX опираются на **exact form** в VocabularyService; BFF не объединяет `sleep` и `slept` на этапе поиска дублей.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Inspector details (Happy Path)

1. **GET** `/api/terms/details?projectId=…&termText=sleep&type=WORD`.
2. **gRPC:** `GetTermDetails` → HTTP **200**.

#### Сценарий Б: Search duplicates before phrase create (Happy Path)

**Сценарий:** User создаёт PHRASE «take off» — проверка exact duplicate.

1. **POST** `/api/terms/search-duplicates` с text «take off», type PHRASE.
2. **gRPC:** `SearchTermDuplicates`.
3. **Ответ:** HTTP **200**, matches list; UI предупреждает о дубле.

#### Сценарий В: Purge demo import (Happy Path)

1. **POST** `/api/terms/purge-demo-import?projectId=…`.
2. **Ответ:** HTTP **200**, `PurgeDemoImportResponseDto` (cardsDeleted, statusesDeleted, termsDeleted).

#### Сценарий Г: Details missing params (Negative Path)

1. **GET** `/api/terms/details` без termText.
2. **BFF:** HTTP **400**.

---

*Следующая группа: [[07 - Подписки на колоды (Deck Subscriptions)]].*
