# Группа 7: Подписки на колоды (Deck Subscriptions)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **VocabularyService** для **подписок пользователя на shared/published колоды** — list, subscribe, unsubscribe.

Подписка связывает user с `deckId` и хранит sync metadata (`lastSyncedVersion`, `subscribedAt`, `lastAccessedAt`, `deckTitle`). Aggregator **не** хранит subscriptions локально и **не** синхронизирует карточки — только REST-фасад для UI library/marketplace.

**Метафора:**

Представьте **журнал подписок на периодические издания в библиотеке**. Вы показываете читательский билет (JWT); библиотекарь (Aggregator) записывает вас на рассылку конкретной колоды или снимает подписку. Содержимое колод и правила доступа хранит центральный каталог (VocabularyService), не стойка.

Identity propagation: [[02 - Контент - проекты и колоды (Content)#SR-AGG-CONTENT-03|SR-AGG-CONTENT-03]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к deck subscriptions.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-SUB-01** | **Подписка на опубликованные колоды:** Список, оформление и отмена подписок текущего пользователя на shared/published decks. |

---

# Детальная спецификация требований

## SR-AGG-SUB-01: List, subscribe, unsubscribe {#SR-AGG-SUB-01}

Управление deck subscriptions **только текущего пользователя**. `userId` берётся из JWT; body не содержит target user. Unsubscribe возвращает **204** при успехе.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **User-scoped** | `MappingHelper.GetUserId` — единственный источник identity; не из body. |
| **201 on subscribe** | `DeckSubscriptionDto` + `CreatedAtAction(nameof(Subscribe), …)`. |
| **204 on unsubscribe** | Успех без body. |
| **JWT обязателен** | `SubscriptionsController` — `[Authorize]` на все actions. |
| **Маппинг ошибок** | gRPC `NotFound` → 404, `PermissionDenied` → 403, `InvalidArgument` → 400. |
| **DTO mapping** | `SubscriptionListItemDto` → `DeckSubscriptionDto` (id, deckId, title, sync fields). |

### 2. Высокоуровневое описание

Представим подписку как **закладку «следить за колодой» в community catalog**.

1. **List (Library sidebar):** пользователь открывает «Мои подписки» — UI запрашивает все deck subscriptions с title и last sync hint.
2. **Subscribe (Follow):** на странице published deck пользователь нажимает Follow — создаётся link user↔deck в Vocabulary.
3. **Unsubscribe:** пользователь снимает закладку — domain удаляет subscription row; BFF подтверждает 204.
4. **Aggregator на каждом шаге:** JWT → userId → gRPC client; **не** проверяет, published ли deck или есть ли entitlement (это downstream + Billing/Community).

Таким образом, Aggregator обеспечивает **минимальный REST CRUD** для subscription UX, не дублируя marketplace rules.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Инициатор:** Frontend (library, marketplace deck page).
* **Controller:** `SubscriptionsController`, base `/api/subscriptions`.
* **Downstream:** `IVocabularyServiceClient.ListSubscriptionsAsync`, `SubscribeAsync`, `UnsubscribeAsync`.
* **JWT:** Bearer обязателен.

#### Сценарий А: Подписка на published deck (Happy Path)

**Сценарий:** Пользователь подписывается на чужую опубликованную колоду.

1. **Запрос (Frontend):** POST `/api/subscriptions/{deckId}` с Bearer JWT.
2. **Identity (BFF):** `GetUserId` → Guid.
3. **gRPC:** `SubscribeAsync(userId, deckId)`.
4. **Маппинг (BFF):** `SubscriptionListItemDto` → `DeckSubscriptionDto`.
5. **Ответ:** HTTP **201 Created**, body с `deckId`, `deckTitle`, `subscribedAt`.

#### Сценарий Б: Список подписок (Happy Path)

**Сценарий:** Dashboard library показывает все followed decks.

1. **GET** `/api/subscriptions`.
2. **gRPC:** `ListSubscriptionsAsync(userId)`.
3. **Ответ:** HTTP **200**, массив `DeckSubscriptionDto`.

#### Сценарий В: Отписка (Happy Path)

1. **DELETE** `/api/subscriptions/{deckId}`.
2. **gRPC:** `UnsubscribeAsync`.
3. **Ответ:** HTTP **204 No Content**.

#### Сценарий Г: Deck не найден (Negative Path)

**Сценарий:** POST subscribe с несуществующим deckId.

1. **gRPC:** `NotFound`.
2. **Ответ (BFF):** HTTP **404**, `{ "error": "<detail>" }`.

#### Сценарий Д: Запрос без JWT (Negative Path)

1. **GET** `/api/subscriptions` без Authorization.
2. **JWT middleware:** HTTP **401**; gRPC не вызывается.

---

*Следующая группа: [[08 - Сообщество и маркетплейс (Community)]].*
