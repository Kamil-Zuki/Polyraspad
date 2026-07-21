# Введение

Группа **Подписки SaaS** описывает жизненный цикл платного (или trial) доступа пользователя к тарифу платформы.

Сущность **`BillingSubscription`** намеренно отделена от deck subscriptions в VocabularyService. Статусы и периоды — проекция доменных событий провайдера + локальный **RenewalWorker** для `LocallyManaged` режима.

---

# SaaS-подписка (`subscriptions`)

## 1. Общее описание

**BillingSubscription** — активная или историческая подписка customer на `SaaSPlan`. Определяет `Status`, billing period, trial window и режим управления renewal.

**Два режима управления (`ManagementMode`):**

| Режим | Провайдеры | Renewal |
| :--- | :--- | :--- |
| `ProviderManaged` | Stripe, Paddle (future) | Webhooks синхронизируют period; worker не нужен. |
| `LocallyManaged` | ЮKassa (v1) | `RenewalWorker` вызывает recurring payment по `current_period_end`. |

## 2. Атрибуты (поля) сущности

**Таблица:** `billing.Subscriptions`

| Название | Тип данных | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `Id` | `uuid` | PK | ID подписки; передаётся в recurring payment metadata. |
| `CustomerId` | `uuid` | FK, NOT NULL | Ссылка на `Customers.Id`. |
| `PlanId` | `uuid` | FK, NOT NULL | Текущий тарифный план. |
| `Provider` | `enum` | NOT NULL | Провайдер, через который оформлена подписка. |
| `ProviderSubscriptionId` | `text` | NULL | ID подписки в провайдере (если есть). |
| `ManagementMode` | `enum` | NOT NULL | `ProviderManaged` \| `LocallyManaged`. |
| `Status` | `enum` | NOT NULL | `Incomplete`, `Trialing`, `Active`, `PastDue`, `Canceled`, `Unpaid`. |
| `CurrentPeriodStart` | `timestamp` | NOT NULL | Начало текущего billing period (UTC). |
| `CurrentPeriodEnd` | `timestamp` | NOT NULL | Конец period; access-check сравнивает с `now`. |
| `TrialStart` | `timestamp` | NULL | Начало trial (если `Trialing`). |
| `TrialEnd` | `timestamp` | NULL | Конец trial. |
| `CancelAtPeriodEnd` | `boolean` | NOT NULL | Флаг «отменить в конце period». |
| `CanceledAt` | `timestamp` | NULL | См. поведение Cancel ниже (в коде семантика не «только immediate cancel»). |
| `CreatedAt` | `timestamp` | NOT NULL | UTC создания. |
| `UpdatedAt` | `timestamp` | NOT NULL | UTC последнего изменения. |

## 3. Связи

| Сущность / сервис | Описание |
| :--- | :--- |
| `Invoices` | Один-ко-многим — платежи по подписке. |
| `Customers`, `Plans` | FK родительские сущности. |
| **AccessService / EntitlementService** | `FindEffectiveSubscription` + grace для `PastDue`. |
| **GetSubscription RPC** | Использует `GetActiveSubscriptionAsync` — только `Active`/`Trialing` + `CurrentPeriodEnd > now` (**не** FindEffective). |
| **WebhookOrchestrator** | Активация/renewal при `PaymentSucceeded`. |

## 4. Жизненный цикл

1. **CreateCheckout** — создаёт subscription `Incomplete`, вызывает provider checkout.
2. **PaymentSucceeded (webhook)** — `Active`, продлевает `CurrentPeriodEnd` (+1 month в v1).
3. **PaymentFailed** — может перевести в `PastDue`.
4. **CancelSubscription** (`SubscriptionService.CancelSubscriptionAsync`) — поведение кода:
   - `cancelAtPeriodEnd=true`: `CancelAtPeriodEnd=true`, **`CanceledAt=UtcNow`**, status остаётся Active/Trialing (доступ до конца period; RenewalWorker skip при флаге).
   - `cancelAtPeriodEnd=false`: `CancelAtPeriodEnd=false`, **`CanceledAt=null`**, `Status=Canceled` сразу.
5. **Grace period** — `PastDue` с `CurrentPeriodEnd` в окне `GracePeriodDays` ещё даёт access (**CheckAccess / GetEntitlements**).
6. **RenewalWorker** — для `LocallyManaged`: recurring charge; skip если `CancelAtPeriodEnd`.

## 5. Effective subscription selection (access / entitlements)

`SubscriptionQueryHelper.FindEffectiveSubscription` (используется **CheckAccess** и **GetEntitlements**, не GetSubscription):

- `Active` или `Trialing` и `CurrentPeriodEnd > now`
- **или** `PastDue` и `CurrentPeriodEnd >= now - GracePeriodDays`
- При нескольких — максимальный `CurrentPeriodEnd`.

Если нет effective subscription → access и entitlements fallback на default plan (`free`).

**GetSubscription:** см. [[03 - Подписки SaaS (Subscriptions)#SR-BILL-SUB-01|SR-BILL-SUB-01]] и ISSUE-004 — UI snapshot уже access-projection.
