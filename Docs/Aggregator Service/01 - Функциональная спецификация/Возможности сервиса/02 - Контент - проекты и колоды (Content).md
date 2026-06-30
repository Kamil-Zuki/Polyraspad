# Группа 2: Контент — проекты и колоды (Content)

## Введение

В этом разделе описывается роль **Aggregator Service** как REST-шлюза к **VocabularyService.ContentService** для управления **языковыми проектами** и **колодами** Polyraspad.

Проект — контейнер верхнего уровня (язык обучения, FSRS-настройки, библиотека). Колоды организуют карточки иерархически (дерево с `parentDeckId`). Вся персистентность и бизнес-правила — в VocabularyService; Aggregator выполняет JWT-авторизацию, извлекает контекст пользователя и проксирует gRPC.

**Метафора:**

Представьте **ресепшен языковой школы с картой этажей**. Ученик не заходит в архив (VocabularyService) — он просит на стойке «открыть курс испанского» или «показать полки в кабинете 3». Ресепшен сверяет пропуск и передаёт заявку в архив; полки, счётчики карточек и права доступа решает архив, не стойка.

REST-контракты: [[04 - Бекенд, API и Контракты/Методы API/REST API/02 - Проекты, колоды и настройки (Content)|REST API — Content]].  
Cross-cutting identity: [[02 - КАР-1 - Thin BFF и REST-to-gRPC маршрутизация]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к REST-шлюзу ContentService.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-CONTENT-01** | **Управление языковыми проектами:** CRUD проектов обучения через REST-фасад к ContentService; шлюз не хранит project локально. |
| **SR-AGG-CONTENT-02** | **Управление колодами и деревом библиотеки:** Иерархия колод, статистика и фильтры каталога; единый REST-фасад к Deck/Tree API. |
| **SR-AGG-CONTENT-03** | **Инъекция identity в downstream-вызовы:** Передача user_id и roles из JWT в gRPC metadata; авторизация и ACL — в VocabularyService. |

---

# Детальная спецификация требований

## SR-AGG-CONTENT-01: CRUD проектов {#SR-AGG-CONTENT-01}

Языковой **проект** — корневая единица workspace в Polyraspad: язык, FSRS-настройки, список колод. Пользователь управляет проектами через REST; Aggregator не хранит projects локально.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Thin BFF** | Aggregator не хранит проекты — только маппинг DTO ↔ `ContentService`. |
| **JWT обязателен** | `ProjectsController` помечен `[Authorize]` целиком. |
| **Ownership downstream** | Доступ к projectId проверяет VocabularyService (`PermissionDenied` → 403). |

### 2. Высокоуровневое описание

Представим проект как **отдельный кабинет в языковой школе**, а Aggregator — **ресепшен, который записывает вас в нужный кабинет**.

1. **Ученик (Frontend):** на dashboard просит «открыть новый курс испанского» или «показать все мои курсы».
2. **Ресепшен (Aggregator):** проверяет JWT — «это действительно Ivan» — и **не решает**, можно ли Ivan создавать курс; просто передаёт заявку с его id.
3. **Администрация школы (VocabularyService / ContentService):** создаёт project, возвращает FSRS settings и metadata.
4. **Ответ ученику:** JSON `ProjectResponseDto` — id, title, language, stats.

Список проектов может включать архивные (`includeArchived=true`) — фильтрация на стороне domain, BFF только прокидывает query.

Таким образом, Aggregator обеспечивает **единый REST-фасад** для project lifecycle, не дублируя бизнес-правила Vocabulary.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `ProjectsController`, base `/api/Projects`.
* **Downstream:** `ContentService` (CreateProject, GetProjects, GetProjectDetails, UpdateProject).
* **Identity:** `MappingHelper.GetUserId` / `GetRoles` → gRPC metadata.

#### Сценарий А: Создание проекта (Happy Path)

**Сценарий:** Пользователь создаёт новый language project с dashboard.

1. **Запрос (Frontend):** POST `/api/Projects`, body `CreateProjectDto`, Bearer JWT.
2. **Identity (BFF):** извлечь userId; маппинг → `CreateProjectRequest`, `UserId` в proto.
3. **gRPC:** `CreateProjectAsync` с metadata `user_id`, `roles`.
4. **Ответ:** HTTP **201 Created**, `ProjectResponseDto`; `Location` через `CreatedAtAction`.

#### Сценарий Б: Список проектов с архивом (Happy Path)

1. **GET** `/api/Projects?includeArchived=true`.
2. **gRPC:** `GetProjects`.
3. **Ответ:** HTTP **200**, массив `ProjectResponseDto`.

#### Сценарий В: Проект не найден (Negative Path)

1. **GET** `/api/Projects/{unknownId}`.
2. **gRPC:** `NotFound` → HTTP **404**.

---

## SR-AGG-CONTENT-02: CRUD колод и дерево колод {#SR-AGG-CONTENT-02}

**Колоды** организуют карточки внутри проекта; дерево (`parentDeckId`) отражает иерархию библиотеки (папки / subdecks). Aggregator предоставляет REST для CRUD и два alias URL для дерева.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Иерархия** | `DeckTreeItemDto` — рекурсивное дерево root decks. |
| **Два URL дерева** | `/api/Projects/{projectId}/decks/tree` и `/api/Decks/tree/{projectId}` → один `GetDeckTree`. |
| **Library filter** | Query `libraryFilter`: `Mine` \| `Downloaded` \| `Public`. |
| **Детали** | GET `/api/Decks/{id}` → stats (new/learning/due/total). |

### 2. Высокоуровневое описание

Представим колоды как **полки и подполки в библиотеке кабинета**.

1. **Читатель (Frontend):** открывает sidebar Library — нужно **дерево полок** (GetDeckTree) или **карточку одной полки** с числом книг (GetDeckDetail).
2. **Библиотекарь (Aggregator):** сверяет JWT, передаёт projectId/deckId и optional filter «только мои / скачанные / публичные».
3. **Архив (VocabularyService):** строит дерево, считает card stats, применяет marketplace metadata (`contributionPolicy`, `licenseType`).
4. **Мутации:** create/update/delete deck — снова через BFF без локального state.

Два URL для дерева существуют для **удобства routing** frontend (project-centric vs deck-centric navigation) — поведение идентично.

Таким образом, Aggregator **не flatten-ит** дерево и **не пересчитывает** stats — только JSON mapping protobuf → DTO.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controllers:** `DecksController` (`/api/Decks`), tree alias на `ProjectsController`.
* **Downstream:** `CreateDeck`, `UpdateDeck`, `DeleteDeck`, `GetDeckDetail`, `GetDeckTree`.

#### Сценарий А: Sidebar deck tree (Happy Path)

1. **GET** `/api/Decks/tree/{projectId}?libraryFilter=Mine`.
2. **Parse filter (BFF):** строка → enum `LibraryFilter.Mine`.
3. **gRPC:** `GetDeckTree` → список `DeckTreeItemDto`.
4. **Ответ:** HTTP **200**.

#### Сценарий Б: Создание subdeck (Happy Path)

1. **POST** `/api/Decks`, body `CreateDeckDto` с `projectId`, optional `parentDeckId`.
2. **gRPC:** `CreateDeck` → HTTP **201**, `DeckResponseDto`.

#### Сценарий В: Удаление колоды (Happy Path)

1. **DELETE** `/api/Decks/{id}`.
2. **gRPC:** `DeleteDeck` → HTTP **204 No Content**.

#### Сценарий Г: Precondition failed (Negative Path)

1. **POST** create deck при нарушении domain rules.
2. **gRPC:** `FailedPrecondition` → HTTP **412**.

---

## SR-AGG-CONTENT-03: Прокидывание user_id и roles в gRPC metadata {#SR-AGG-CONTENT-03}

Cross-cutting требование для **всех** Content-вызовов: downstream должен знать **кто** делает запрос, без доверия к полям body.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Единый паттерн** | `IVocabularyServiceClient.*Async(..., userId, roles, ct)`. |
| **Claims source** | JWT `sub`, `role` claims через `MappingHelper`. |
| **Test fallback** | `X-User-Id`, `X-User-Roles` — только integration tests. |
| **Без подмены** | userId из body/query **не** авторитетен для RBAC. |

### 2. Высокоуровневое описание

Представим metadata как **шёпот охранника архivist-у на ухо**: «этот запрос от Ivan, роли: User».

1. **Клиент:** отправляет Bearer JWT — единственный доверенный источник identity на BFF.
2. **Aggregator:** после middleware извлекает Guid и список roles.
3. **gRPC channel:** каждый вызов ContentService получает metadata `user_id` (+ roles по контракту client).
4. **Protobuf request:** часто дублирует `UserId` string в message (например `CreateProjectRequest`).
5. **VocabularyService:** решает PermissionDenied / фильтрует «мои проекты» — BFF **не интерпретирует** RBAC.

Таким образом, даже если злоумышленник подставит чужой `projectId` в URL, решение «можно / нельзя» принимает **domain-сервис**, опираясь на metadata от BFF, а не на client-supplied user id.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Helper:** `MappingHelper.GetUserId`, `GetRoles`.
* **Client:** `VocabularyServiceClient` добавляет metadata на каждый RPC.

#### Сценарий А: Запрос без JWT (Negative Path)

1. **GET** `/api/Projects` без Authorization.
2. **JWT middleware:** HTTP **401**; gRPC **не вызывается**.

#### Сценарий Б: Доступ к чужому проекту (Negative Path)

1. **GET** `/api/Projects/{foreignId}` с JWT пользователя A.
2. **Metadata:** user_id = A.
3. **gRPC:** `PermissionDenied` → HTTP **403**.

---

*Следующая группа: [[03 - Карточки и редактор (Cards)]].*
