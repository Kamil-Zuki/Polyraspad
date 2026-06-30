# Группа 3: Подписки SaaS (Subscriptions)

## Введение

В этом разделе описывается **жизненный цикл SaaS-подписки**: чтение текущего состояния, создание checkout и отмена.

Подписка (`BillingSubscription`) — не deck subscription. Checkout создаёт `Incomplete` subscription и redirect URL провайдера; активация — через webhook [[07 - Webhook-оркестрация (Webhooks)]].

**Метафора:**

Представьте **абонемент в спортзал**. Оформление (checkout) — бумага заявки; оплата на кассе провайдера; после чека абонемент активируется в базе. Отмена — «не продлевать с следующего месяца» или мгновенное прекращение.

Сущности: [[Entity - Подписки SaaS - Subscriptions]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к подпискам SaaS.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-SUB-01** | **Get subscription:** Текущая подписка пользователя — plan_code, status, period, trial, cancel flags. |
| **SR-BILL-SUB-02** | **Create checkout:** Создание payment session у провайдера; subscription Incomplete + checkout URL. |
| **SR-BILL-SUB-03** | **Cancel subscription:** cancel_at_period_end или immediate; обновление статуса в БД. |

---

# Детальная спецификация требований

## SR-BILL-SUB-01: Get subscription {#SR-BILL-SUB-01}

UI billing dashboard и `SubscriptionBadge` читают актуальный subscription snapshot.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **User-scoped** | Запрос по `user_id` из caller; не по subscription id из клиента. |
| **Effective row** | Возвращается последняя релевантная подписка customer или empty. |
| **Proto Subscription** | Поля mirror entity: status, periods, trial, cancel_at_period_end. |

### 2. Высокоуровневое описание

Представим текущую подписку как **штамп в абонементе спортзала**.

1. **Запрос после login:** Aggregator вызывает `GetSubscription(user_id)` — клиент не передаёт subscription id.
2. **Выбор effective row:** Billing находит последнюю релевантную `BillingSubscription` customer или возвращает empty snapshot.
3. **Отображение в UI:** `SubscriptionBadge` показывает Pro до `current_period_end` или Free, если paid row отсутствует или canceled.

Таким образом, `GetSubscription` — read-only снимок жизненного цикла подписки для dashboard и badge без side effects.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Active Pro user (Happy Path)

1. **gRPC:** `GetSubscription(user_id)`.
2. **Ответ:** `status=active`, `plan_code=pro`, `current_period_end` в будущем.

#### Сценарий Б: Free user без paid subscription (Happy Path)

1. **Ответ:** пустой `subscription` или canceled row; UI fallback на free badge.

---

## SR-BILL-SUB-02: Create checkout {#SR-BILL-SUB-02}

Upgrade flow: пользователь выбирает `pro` → redirect на ЮKassa (или mock URL в dev).

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Provider override** | Optional `provider` в request; default из config. |
| **LocallyManaged for YooKassa** | `ManagementMode = LocallyManaged` при checkout. |
| **save_payment_method** | ЮKassa checkout с сохранением карты для renewal. |
| **Incomplete first** | Subscription row создаётся до успешной оплаты. |

### 2. Высокоуровневое описание

Представим checkout как **оформление заявки на абонемент с оплатой на кассе провайдера**.

1. **Подготовка customer:** `EnsureCustomer` + bind `provider` (default или override из request).
2. **Заявка Incomplete:** INSERT `BillingSubscription` со статусом `Incomplete` до успешной оплаты.
3. **Сессия провайдера:** `IPaymentProvider.CreateCheckoutAsync` с `save_payment_method` → `checkout_url` и `provider_payment_id`.
4. **Redirect:** browser переходит на ЮKassa или mock URL; return URL ведёт на `/billing/success`.
5. **Активация:** webhook `payment.succeeded` переводит подписку в Active и продлевает period.

Таким образом, checkout создаёт «бумажную заявку» и payment session; факт paid subscription фиксируется только через webhook pipeline.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Upgrade to Pro (Happy Path)

1. **REST:** POST `/api/billing/checkout` body `{ planCode: "pro" }`.
2. **gRPC:** `CreateCheckout`.
3. **Ответ:** `checkout_url` — frontend `window.location`.

#### Сценарий Б: Unknown plan code (Negative Path)

1. **gRPC:** plan not found / inactive.
2. **Ответ:** error mapped to 400 на Aggregator.

---

## SR-BILL-SUB-03: Cancel subscription {#SR-BILL-SUB-03}

Пользователь отменяет Pro — обычно `cancel_at_period_end=true` чтобы сохранить access до конца period.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **cancel_at_period_end** | Default UX — доступ до `current_period_end`. |
| **Immediate cancel** | `cancel_at_period_end=false` — status Canceled сразу. |
| **Renewal skip** | Worker не продлевает если `CancelAtPeriodEnd` или Canceled. |

### 2. Высокоуровневое описание

Представим отмену подписки как **заявку «не продлевать абонемент» у администратора зала**.

1. **Запрос пользователя:** UI вызывает `CancelSubscription` с `cancel_at_period_end=true` (default) или immediate cancel.
2. **Обновление flags:** Billing выставляет `CancelAtPeriodEnd` или переводит status в `Canceled` сразу.
3. **Renewal skip:** `RenewalWorker` не продлевает подписку при `CancelAtPeriodEnd` или уже canceled status.
4. **Провайдер v1:** для ЮKassa cancel может быть локальным only — без вызова provider API cancel.

Таким образом, пользователь сохраняет Pro-доступ до конца оплаченного period при soft cancel; immediate cancel прекращает paid snapshot сразу.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Cancel at period end (Happy Path)

1. **gRPC:** `CancelSubscription(cancel_at_period_end=true)`.
2. **DB:** `CancelAtPeriodEnd=true`; status остаётся Active до period end.
3. **UI:** «Pro до 15 июля».

---

*Следующая группа: [[04 - Access и entitlements]].*
