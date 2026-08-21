# Группа 8: Сообщество и маркетплейс (Community)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **VocabularyService.CommunityService** — **collaborative contributions**, **publish/fork decks**, **author profiles** и **marketplace** (products, reviews, stats, deck entitlement).

Все маршруты на `CommunityController` (`/api/...`) под `[Authorize]`. Moderation, pricing, fork lineage и entitlement logic — в VocabularyService; Aggregator выполняет JWT, metadata и AutoMapper DTO ↔ protobuf.

**Метафора:**

Представьте **редакцию открытого учебника с магазином приложений**. Авторы предлагают правки (contributions), выкладывают колоды в каталог, читатели форкают работы; premium decks продаются через marketplace. Aggregator — **приёмная редакции**: принимает заявки с проверкой пропуска и передаёт их в издательство (CommunityService).

REST-контракты: `04/.../REST API/` (Community — при расширении 04).

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к community и marketplace.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-COMM-01** | **Коллаборативные правки (contributions):** Создание, просмотр и разбор предложений изменений deck и настройка contribution policy. |
| **SR-AGG-COMM-02** | **Публикация и fork колод:** Публикация deck в community catalog, fork в project пользователя, browse и author profile. |
| **SR-AGG-COMM-03** | **Маркетплейс учебного контента:** Products, отзывы, статистика продавца и проверка deck entitlement перед premium access. |

---

# Детальная спецификация требований

## SR-AGG-COMM-01: Contributions {#SR-AGG-COMM-01}

Collaborative editing: участник предлагает изменение карточки/deck; maintainer deck approve/reject через resolve.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Dual list routes** | GET `/api/decks/{deckId}/contributions` и GET `/api/contributions` («мои предложения»). |
| **Create + fetch** | После `CreateContribution` BFF вызывает `GetContribution` для полного DTO в 201. |
| **Resolve** | POST `/api/contributions/{id}/resolve` — approve/reject с resolution payload. |
| **Policy** | PUT `/api/decks/{deckId}/contribution-policy` — кто может предлагать правки. |
| **Pagination** | List endpoints → `PaginatedResponseDto<ContributionResponseDto>`. |
| **Identity** | metadata `user_id`, `roles` на каждый gRPC call. |

### 2. Высокоуровневое описание

Представим contributions как **pull request в GitHub для колоды**.

1. **Create:** contributor отправляет предложение (новая карточка, правка поля) с `deckId` и `type`.
2. **List/Get:** deck owner видит очередь pending; contributor — «мои» через альтернативный URL.
3. **Resolve:** maintainer принимает или отклоняет — domain применяет или отклоняет изменение атомарно.
4. **Policy:** owner настраивает open/closed/restricted contribution mode до flood PR.

Aggregator **не** мержит карточки сам — только транспорт resolution decision.

Таким образом, community editing остаётся **auditable workflow** в Vocabulary, а REST — набор thin endpoints для moderation UI.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `CommunityController`, routes под `/api/contributions`, `/api/decks/{deckId}/contributions`.
* **Downstream:** CommunityService gRPC.

#### Сценарий А: Создание contribution (Happy Path)

**Сценарий:** Участник предлагает новую карточку в shared deck.

1. **POST** `/api/contributions`, body `CreateContributionDto` (deckId, type, payload).
2. **gRPC:** `CreateContribution` → `GetContribution`.
3. **Ответ:** HTTP **201**, `ContributionResponseDto`; `Location` на get by id.

#### Сценарий Б: Resolve approve (Happy Path)

1. **POST** `/api/contributions/{id}/resolve` с resolution approve.
2. **gRPC:** `ResolveContribution`.
3. **Ответ:** HTTP **200**, updated contribution DTO.

#### Сценарий В: Нет прав на resolve (Negative Path)

1. **gRPC:** `PermissionDenied`.
2. **Ответ (BFF):** HTTP **403**.

---

## SR-AGG-COMM-02: Publish, fork, published catalog {#SR-AGG-COMM-02}

Публикация колоды в community catalog, fork published deck, browse catalog и author pages.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Publish** | POST `/api/decks/{deckId}/publish` — deck становится discoverable. |
| **Fork** | POST `/api/decks/{deckId}/fork` — копия в project пользователя с lineage. |
| **Catalog** | GET `/api/decks/published` — pagination, search filters по контракту DTO. |
| **Author** | GET `/api/authors/{authorId}` — public profile + published decks summary. |
| **Ownership** | Publish/fork проверяет domain; BFF не override RBAC. |

### 2. Высокоуровневое описание

Представим publish/fork как **выкладку книги в публичную библиотеку и копирование на домашнюю полку**.

1. **Publish:** автор делает deck visible в community index — metadata, license, stats.
2. **Fork:** читатель копирует published deck в свой project — новый deckId, ссылка на source.
3. **Browse:** marketplace/library UI листает `/decks/published` с filters.
4. **Author page:** публичный профиль автора для social discovery.

Aggregator не индексирует full-text — query params → protobuf filters.

Таким образом, **discovery UX** строится на REST catalog, а **content ownership** остаётся в Vocabulary.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Routes:** `/api/decks/{deckId}/publish`, `/fork`, `/api/decks/published`, `/api/authors/{authorId}`.

#### Сценарий А: Fork published deck (Happy Path)

**Сценарий:** Пользователь добавляет чужую колоду в свой project через fork.

1. **POST** `/api/decks/{deckId}/fork`, optional body с target projectId.
2. **gRPC:** fork RPC в CommunityService.
3. **Ответ:** HTTP **201**, новый deck id и metadata.

#### Сценарий Б: Browse published catalog (Happy Path)

1. **GET** `/api/decks/published?page=1&pageSize=20`.
2. **Ответ:** HTTP **200**, paginated published decks.

#### Сценарий В: Publish без прав (Negative Path)

1. **POST** publish от non-owner.
2. **gRPC:** `PermissionDenied` → HTTP **403**.

---

## SR-AGG-COMM-03: Marketplace {#SR-AGG-COMM-03}

Monetized decks: products, reviews, stats, entitlement check перед доступом к premium content.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Products CRUD** | POST/PUT/GET `/api/marketplace/products`, GET by id. |
| **Reviews** | POST `/api/marketplace/products/{id}/reviews`. |
| **Stats** | GET `/api/marketplace/products/{id}/stats` — seller analytics. |
| **Entitlement** | GET `/api/decks/{deckId}/entitlement` — hasAccess для current user. |
| **Billing separation** | Оплата через [[10 - SaaS-биллинг (Billing)|Billing]]; Community только entitlement read. |

### 2. Высокоуровневое описание

Представим marketplace как **витрину App Store для колод**.

1. **Seller** создаёт product, привязанный к deck, задаёт price metadata.
2. **Buyer** покупает через Billing checkout; webhook обновляет entitlement downstream.
3. **Before study/import:** UI вызывает entitlement — можно ли открыть deck.
4. **Reviews/stats** — social proof и seller dashboard.

Aggregator проксирует CRUD и read checks; **не** обрабатывает платежи.

Таким образом, monetization split: **Billing** = money, **Community** = access flags, **Aggregator** = REST glue.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Entitlement check before open (Happy Path)

**Сценарий:** UI проверяет доступ к premium deck перед import.

1. **GET** `/api/decks/{deckId}/entitlement`.
2. **gRPC:** entitlement lookup с user metadata.
3. **Ответ:** HTTP **200**, `{ hasAccess: true/false, … }`.

#### Сценарий Б: Create marketplace product (Happy Path)

1. **POST** `/api/marketplace/products`, body product DTO.
2. **Ответ:** HTTP **201**, product id.

#### Сценарий В: Review without purchase (Negative Path)

1. **POST** review когда domain требует verified purchase.
2. **gRPC:** `FailedPrecondition` или `PermissionDenied` → HTTP **412** или **403**.

---

*Следующая группа: [[09 - Медиа и Reader Library (Media)]].*
