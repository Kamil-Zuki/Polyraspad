# Введение

Методы данной группы описывают **жизненный цикл SaaS-подписки**: чтение текущего состояния, создание checkout session и отмена.

Подписка (`BillingSubscription`) — не deck subscription (VocabularyService). Checkout создаёт `Incomplete` subscription и redirect URL; активация — через webhook [[06 - Webhook-оркестрация (Webhooks)]].

**SR группы:** **SR-BILL-SUB-01**, **SR-BILL-SUB-02**, **SR-BILL-SUB-03**. Сущности: [[Entity - Подписки SaaS - Subscriptions]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-BILL-SUB-01 | `GetSubscription` | Unary | Текущая подписка пользователя — plan_code, status, period, cancel flags. |
| SR-BILL-SUB-02 | `CreateCheckout` | Unary | Checkout session у провайдера; subscription Incomplete + checkout URL. |
| SR-BILL-SUB-03 | `CancelSubscription` | Unary | Отмена at period end или immediate. |

---

<span id="grpc-GetSubscription"></span>

# SR-BILL-SUB-01: Get subscription: GetSubscription

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Подписки SaaS (Subscriptions)#SR-BILL-SUB-01]]

UI billing dashboard и `SubscriptionBadge` читают snapshot подписки. Запрос по `user_id` — клиент не передаёт subscription id.

**REST-паритет:** `GET /api/Billing/subscription` (Aggregator, JWT → user_id).

| Сигнатура | `rpc GetSubscription(GetSubscriptionRequest) returns (GetSubscriptionResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `GetSubscriptionRequest` — `user_id` (UUID string) |
| **Сообщение ответа** | `GetSubscriptionResponse` — `Subscription subscription` (optional; пустой если нет effective row) |

## Логика обработки запроса

1. Распарсить `user_id`; при ошибке — `INVALID_ARGUMENT`.
2. `SubscriptionService.GetActiveSubscriptionAsync(userId)`:
   - JOIN `subscriptions` → `customers` WHERE `Customer.UserId = user_id`;
   - фильтр `Status IN (Active, Trialing)` AND `CurrentPeriodEnd > now`;
   - ORDER BY `CurrentPeriodEnd` DESC; FIRST.
3. Если строка найдена — смапить entity → proto `Subscription` (status/provider lowercase, timestamps UTC).
4. Если нет — вернуть `GetSubscriptionResponse` с пустым `subscription` (UI fallback на free badge).

> **Примечание:** `CheckAccess` / `GetEntitlements` используют `SubscriptionQueryHelper` с grace для `PastDue`; `GetSubscription` — более узкий фильтр только Active/Trialing. См. ISSUE-002 в `99 - Staging`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный `user_id`. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

<span id="grpc-CreateCheckout"></span>

# SR-BILL-SUB-02: Create checkout: CreateCheckout

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Подписки SaaS (Subscriptions)#SR-BILL-SUB-02]]

Upgrade flow: пользователь выбирает `pro` → redirect на ЮKassa (или mock URL в dev). Для ЮKassa: `ManagementMode = LocallyManaged`, checkout с `save_payment_method`.

**REST-паритет:** `POST /api/Billing/checkout` body `{ planCode }` — Aggregator передаёт `user_id` и `email` из JWT claims.

| Сигнатура | `rpc CreateCheckout(CreateCheckoutRequest) returns (CreateCheckoutResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `CreateCheckoutRequest` — `user_id`, `email`, `plan_code`, optional `provider`, optional `return_url` |
| **Сообщение ответа** | `CreateCheckoutResponse` — `checkout_url`, `provider_payment_id` |

## Логика обработки запроса

1. Распарсить `user_id`; валидировать `plan_code` не пустой.
2. Найти `SaaSPlan` по `Code = plan_code` AND `IsActive = true`; если не найден — `InvalidOperationException` → gRPC **INTERNAL** (Aggregator maps to HTTP 500/400).
3. Определить provider: `request.provider` или `Billing:DefaultProvider`; получить `IPaymentProvider` через factory.
4. Вызвать `EnsureCustomer(user_id, email)` — upsert `customers`, обновить `Provider` и email.
5. INSERT `BillingSubscription`:
   - `Status = Incomplete`
   - `ManagementMode = LocallyManaged` для `yookassa`, иначе `ProviderManaged`
   - `CurrentPeriodStart = now`; `CurrentPeriodEnd` = now + trial_days или +1 month
   - `TrialStart`/`TrialEnd` если `plan.TrialDays > 0`
6. Вызвать `IPaymentProvider.CreateCheckoutAsync` с price, currency, customer metadata, `return_url`.
7. Сохранить `ProviderSubscriptionId` / payment id на subscription; вернуть `checkout_url` и `provider_payment_id`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный `user_id` или пустой `plan_code`. |
| **INTERNAL** | План не найден / неактивен (`InvalidOperationException`); неподдерживаемый `provider` (`NotSupportedException`); ошибка БД или HTTP к payment provider. |

---

<span id="grpc-CancelSubscription"></span>

# SR-BILL-SUB-03: Cancel subscription: CancelSubscription

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Подписки SaaS (Subscriptions)#SR-BILL-SUB-03]]

Отмена Pro — обычно `cancel_at_period_end=true` для сохранения access до `current_period_end`. `RenewalWorker` не продлевает подписки с `CancelAtPeriodEnd`.

**REST-паритет:** `POST /api/Billing/subscription/cancel` — при отсутствии active subscription Aggregator возвращает HTTP **404**.

| Сигнатура | `rpc CancelSubscription(CancelSubscriptionRequest) returns (CancelSubscriptionResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `CancelSubscriptionRequest` — `user_id`, `cancel_at_period_end` (bool) |
| **Сообщение ответа** | `CancelSubscriptionResponse` — `Subscription subscription` (optional) |

## Логика обработки запроса

1. Распарсить `user_id`.
2. Найти последнюю subscription customer со status `Active` или `Trialing` (ORDER BY `CurrentPeriodEnd` DESC).
3. Если не найдена — вернуть response с пустым `subscription` (Aggregator → 404).
4. Установить `CancelAtPeriodEnd = request.cancel_at_period_end`.
5. Если `cancel_at_period_end = false`: немедленно `Status = Canceled`, `CanceledAt = now`.
6. Если `cancel_at_period_end = true`: status остаётся Active до period end; `CanceledAt = now` (фиксация intent).
7. Обновить `UpdatedAt`; вернуть обновлённый proto `Subscription`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный `user_id`. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

*Следующая группа: [[04 - Access и entitlements]].*
