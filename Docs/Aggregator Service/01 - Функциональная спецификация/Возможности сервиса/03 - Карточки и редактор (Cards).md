# Группа 3: Карточки и редактор (Cards)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **VocabularyService.CardService** — операции с **учебными карточками** (Anki-like notes), поиск, импорт и метаданные редактора.

Карточки живут внутри колод проекта; FSRS-состояние и note fields управляются VocabularyService. Aggregator обеспечивает JSON API для frontend, Card Editor и browser extension capture flows.

**Метафора:**

Представьте **окно выдачи каталожных карточек в архиве**. Читатель заполняет бланк (note fields); окно (Aggregator) принимает бланк, проверяет пропуск и передаёт его в хранилище (CardService). Поиск по каталогу, пакетный импорт и «захват» страницы из extension — те же окна, разные формы заявок.

REST-контракты: [[04 - Бекенд, API и Контракты/Методы API/REST API/|REST API — Cards]] (детальные endpoint blocks).  
Identity propagation: SR-AGG-CONTENT-03 (metadata `user_id`, `roles`).

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к REST-прокси CardService.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-CARD-01** | **CRUD учебной карточки:** Создание, чтение, обновление и удаление note/card через CardService. |
| **SR-AGG-CARD-02** | **Поиск, захват и импорт карточек:** Full-text search, проверка exact-дубликатов, capture из extension и пакетный import в колоду. |
| **SR-AGG-CARD-03** | **Схема note type для редактора:** Динамические поля и Anki-like templates для Card Editor без hardcoded schema во frontend. |
| **SR-AGG-CARD-04** | **Обслуживание карточек:** bulk-delete, move, bulk-reset-progress, leeches, missing-media. |

---

# Детальная спецификация требований

## SR-AGG-CARD-01: Создание, обновление и получение карточки {#SR-AGG-CARD-01}

Базовый CRUD карточки — ручное создание в редакторе, чтение для preview/study bridge, обновление полей note. Aggregator не валидирует FSRS и не рендерит шаблоны — это domain VocabularyService.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Thin BFF** | Note validation, SRS state — в VocabularyService. |
| **JWT обязателен** | `CardsController` — `[Authorize]` на все actions. |
| **RESTful ids** | `cardId`, `deckId` — string GUID в URL/body. |

### 2. Высокоуровневое описание

Представим карточку как **каталожную карточку книги в архиве**, а Aggregator — **окно выдачи**.

1. **Читатель (Frontend / Editor):** заполняет поля note (Front, Back, …), указывает список тегов (tags) и нажимает Save — «хочу новую карточку на полке X».
2. **Окно выдачи (Aggregator):** проверяет JWT, упаковывает поля и теги в `CreateCardRequest`, **не проверяя**, дубликат ли это.
3. **Архив (CardService):** создаёт note+card, привязывает теги (через связующие сущности в Vocabulary Core), инициализирует SRS, возвращает `CardResponseDto` с массивом назначенных тегов.
4. **Чтение / правка:** GET по id или PUT update (с обновлением списка тегов) — тот же путь: JWT → metadata → gRPC → JSON.

Aggregator не хранит черновики карточек между запросами — каждая операция stateless.

Таким образом, Card Editor общается с **одним REST-контрактом**, не зная про gRPC и protobuf Vocabulary.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `CardsController`, base `/api/Cards`.
* **Downstream:** `CreateCard`, `GetCard`, `UpdateCard`.
* **DTO:** `CreateCardDto`, `UpdateCardDto`, `CardResponseDto`.

#### Сценарий А: Создание в редакторе (Happy Path)

**Сценарий:** Пользователь сохраняет новую карточку в deck.

1. **POST** `/api/Cards`, body с `deckId` и note field values.
2. **Identity + gRPC:** metadata → `CreateCard`.
3. **Ответ:** HTTP **201 Created**, `CardResponseDto`.

#### Сценарий Б: Открытие карточки (Happy Path)

1. **GET** `/api/Cards/{id}`.
2. **gRPC:** `GetCard` → HTTP **200**.

#### Сценарий В: Карточка не найдена (Negative Path)

1. **GET** `/api/Cards/{unknownId}`.
2. **gRPC:** `NotFound` → HTTP **404**.

---

## SR-AGG-CARD-02: Поиск, проверка дубликатов, capture и массовый импорт {#SR-AGG-CARD-02}

Помимо одиночного CRUD, карточки **ищут**, **импортируют пакетом**, **захватывают из внешнего контекста** (extension, reader) и **проверяют дубликаты** перед созданием.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Search** | Full-text + pagination (`pageNumber`, `pageSize` default 20). |
| **Scope** | Filters: `projectId`, `deckId`, `srsStatuses[]`, `tags[]`. |
| **Capture** | Отдельный RPC для external/mining context. |
| **Bulk import** | POST `/api/Cards/import` — many cards одной колоде. |
| **Duplicates** | POST `check-duplicates` до create. |

### 2. Высокоуровневое описание

Представим расширенные операции как **разные окна одного архива**.

1. **Поиск (Search):** библиотекарь принимает ключевые слова, **список тегов** и **номер страницы каталога** — Aggregator передаёт query и tags в `SearchCards`, Vocabulary возвращает page + total count.
2. **Проверка дубликатов:** перед добавлением книги клиент спрашивает «нет ли уже такой на полке?» — `CheckCardDuplicates`, ответ без создания.
3. **Capture:** extension «бросает» на стойку фото страницы/субтитры — `CaptureCard` с source metadata; domain решает note shape.
4. **Bulk import:** CSV/JSON batch — один gRPC `BulkCreateCards`; BFF возвращает список созданных id.

Aggregator **не** выполняет full-text indexing — только query params → protobuf.

Таким образом, все «массовые» и «внешние» сценарии сходятся в **одном CardService**, а REST остаётся набором thin endpoints.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Base:** `/api/Cards/...`
* **Pagination DTO:** `PaginatedResponseDto<CardResponseDto>`.

#### Сценарий А: Global search (Happy Path)

1. **GET** `/api/Cards/search?query=hello&projectId={id}&pageNumber=1&pageSize=20`.
2. **Parse srsStatuses (BFF):** optional array → enum list в protobuf.
3. **gRPC:** `SearchCards`.
4. **Ответ:** HTTP **200**, paginated items + `totalCount`.

#### Сценарий Б: Capture из extension (Happy Path)

1. **POST** `/api/Cards/capture`, body `CaptureCardDto` (projectId, source, fields).
2. **gRPC:** `CaptureCard`.
3. **Ответ:** HTTP **201**, `CardResponseDto`.

#### Сценарий В: Bulk import (Happy Path)

1. **POST** `/api/Cards/import`, body `BulkCreateCardsDto` с массивом cards.
2. **gRPC:** `BulkCreateCards`.
3. **Ответ:** HTTP **200**, список `CardResponseDto`.

#### Сценарий Г: Payload too large (Negative Path)

1. **POST** capture с чрезмерным content/media.
2. **gRPC:** `ResourceExhausted` → HTTP **413 Payload Too Large**.

---

## SR-AGG-CARD-03: Note type для редактора {#SR-AGG-CARD-03}

Card Editor должен **динамически** знать, какие поля note и какие front/back templates доступны в проекте — схема живёт в Vocabulary, BFF отдаёт её JSON для UI.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Dynamic editor** | Field definitions + card templates per project. |
| **Validation на BFF** | `projectId` — обязательный valid GUID; иначе 400 без gRPC. |
| **Downstream schema** | `GetNoteTypeForEditor` в CardService. |

### 2. Высокоуровневое описание

Представим note type как **бланк анкеты**, который архив выдаёт редактору **до** заполнения.

1. **Редактор (Frontend):** открывает `/projects/{id}/editor/new` — «какие поля мне показать?».
2. **Окно (Aggregator):** проверяет, что projectId — GUID; иначе сразу 400 (защита от мусорных запросов).
3. **Архив (CardService):** возвращает `NoteFieldDefinitionDto[]`, `CardTemplateDto[]` для Anki-like preview.
4. **UI:** строит form и live preview front/back **без hardcoded schema** в frontend.

Aggregator не кэширует schema между пользователями — каждый open editor = fresh gRPC (при необходимости frontend кэширует в React Query).

Таким образом, изменение note type в Vocabulary **автоматически** отражается в Editor после следующего GET — без деплоя Aggregator.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** GET `/api/Cards/note-type/editor?projectId={guid}`.
* **Downstream:** gRPC `GetNoteTypeForEditor`.
* **Ответ:** `GetNoteTypeForEditorResponseDto`.

#### Сценарий А: Открытие Card Editor (Happy Path)

**Сценарий:** Пользователь создаёт новую карточку в project.

1. **GET** note-type/editor с valid projectId + Bearer.
2. **gRPC:** `GetNoteTypeForEditor`.
3. **Ответ:** HTTP **200**; UI рендерит fields и templates.

#### Сценарий Б: Invalid projectId (Negative Path)

1. **GET** с `projectId=not-a-guid`.
2. **BFF validation:** HTTP **400** `{ "error": "Invalid projectId" }` — gRPC не вызывается.

#### Сценарий В: Нет доступа (Negative Path)

1. **GET** с чужим projectId.
2. **gRPC:** `PermissionDenied` → HTTP **403**.

---

## SR-AGG-CARD-04: Обслуживание карточек {#SR-AGG-CARD-04}

Пакетные и служебные операции CardService, экспонированные в REST.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| Thin BFF | Логика фильтрации leeches / missing media — в Vocabulary. |
| JWT | Все маршруты `[Authorize]`. |

### 2. Высокоуровневое описание

REST-поверхность включает delete одной карточки, bulk-delete, move между колодами, bulk-reset-progress, списки leeches и missing-media.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Bulk delete (Happy Path)
1. Клиент вызывает bulk-delete с массивом cardId.
2. BFF проксирует gRPC; возвращает результат удаления.

---

*Следующая группа по порядку в `00 - Общая информация`: **4. Сессии обучения (Study)**.*
