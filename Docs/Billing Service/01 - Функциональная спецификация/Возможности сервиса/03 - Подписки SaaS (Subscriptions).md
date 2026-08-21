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

UI billing dashboard и `SubscriptionBadge` читают subscription snapshot через `GetActiveSubscriptionAsync`.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **User-scoped** | Запрос по `user_id` из caller; не по subscription id из клиента. |
| **Active/Trialing only** | Код: status ∈ {`Active`, `Trialing`} и `CurrentPeriodEnd > now`; order by period end DESC; first or empty. |
| **Not FindEffective** | `PastDue` + grace **не** включаются (в отличие от CheckAccess / GetEntitlements). См. ISSUE-004. |
| **Proto Subscription** | Поля mirror entity: status, periods, trial, cancel_at_period_end, canceled_at. |

### 2. Высокоуровневое описание

Представим текущую подписку как **штамп «активный абонемент»** — не полный access-check.

1. **Запрос после login:** Aggregator вызывает `GetSubscription(user_id)`.
2. **Выбор row:** `GetActiveSubscriptionAsync` — только Active/Trialing с period в будущем; иначе empty.
3. **UI:** badge Pro при наличии row; иначе Free. PastDue в grace может всё ещё давать access через CheckAccess, но snapshot пустой/без PastDue.

Таким образом, `GetSubscription` — узкий UI snapshot, уже projection access/entitlements.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Active Pro user (Happy Path)

1. **gRPC:** `GetSubscription(user_id)`.
2. **Ответ:** `status=active`, `plan_code=pro`, `current_period_end` в будущем.

#### Сценарий Б: Free / no Active-Trialing row (Happy Path)

1. Нет Active/Trialing с будущим period (в т.ч. только PastDue или Canceled).
2. **Ответ:** empty subscription; UI Free badge (access может отличаться — ISSUE-004).

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
| **cancel_at_period_end=true** | `CancelAtPeriodEnd=true`, **`CanceledAt=UtcNow`**, status остаётся Active/Trialing. |
| **Immediate (false)** | `CancelAtPeriodEnd=false`, **`CanceledAt=null`**, `Status=Canceled`. |
| **Renewal skip** | Worker не продлевает если `CancelAtPeriodEnd`. |

### 2. Высокоуровневое описание

Представим отмену подписки как **заявку «не продлевать абонемент»**.

1. **Period-end cancel:** flags `CancelAtPeriodEnd` + timestamp в `CanceledAt` (код ставит now при soft cancel); status не меняется до конца period / webhook.
2. **Immediate cancel:** status → `Canceled`, `CanceledAt` очищается (`null`).
3. **Renewal skip:** `RenewalWorker` фильтрует `!CancelAtPeriodEnd`.
4. **Провайдер v1:** ЮKassa cancel может быть локальным only.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Cancel at period end (Happy Path)

1. **gRPC:** `CancelSubscription(cancel_at_period_end=true)`.
2. **DB:** `CancelAtPeriodEnd=true`, `CanceledAt=now`, status Active/Trialing.
3. **UI:** «Pro до {current_period_end}».

---

*Следующая группа: [[04 - Access и entitlements]].*
