# Введение

Настоящий документ описывает **gRPC** интерфейс микросервиса **Billing Service** — единственный машинный контракт сервиса. Публичный REST для браузера — на **Aggregator Service** (`/api/Billing/*`); **VocabularyService** и Aggregator вызывают Billing по gRPC (порт **5127**, h2c в Docker).

Billing владеет PostgreSQL: plans, customers, subscriptions, invoices, webhook idempotency. Payment providers (Mock, ЮKassa) — адаптеры внутри процесса; webhook ingress проксируется Aggregator → `ProcessWebhook`.

**Proto source of truth:** `BillingService/Protos/billing.proto` (копия — [[billing.proto]]). Package: `pvs.billing.v1`, C# namespace `Pvs.Billing.Grpc`.

**REST mapping:** `Docs/Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/10 - SaaS-биллинг (Billing).md`.

**Liveness:** `GET /healthz` на порту 5127 — см. [[09 - Платформенные контракты (Operations)]].

---

# 1. Группы методов gRPC

| Группа | Файл | RPC |
| :--- | :--- | :---: |
| Управление клиентами (Customers) | [[01 - Управление клиентами (Customers)]] | 1 |
| Каталог SaaS-планов (Plans) | [[02 - Каталог SaaS-планов (Plans)]] | 1 |
| Подписки SaaS (Subscriptions) | [[03 - Подписки SaaS (Subscriptions)]] | 3 |
| Access и entitlements | [[04 - Access и entitlements]] | 2 |
| Инвойсы (Invoices) | [[05 - Инвойсы (Invoices)]] | 1 |
| Webhook-оркестрация (Webhooks) | [[06 - Webhook-оркестрация (Webhooks)]] | 1 |
| Платёжные провайдеры (Payment Providers) | — | 0 (адаптеры внутри сервиса) |
| Автопродление (Renewal) | — | 0 (`RenewalWorker`, без RPC) |
| Платформенные контракты (Operations) | [[09 - Платформенные контракты (Operations)]] | 0 (HTTP health) |

**Итого:** 9 unary RPC в `billing.proto`.

---

# 2. Управление клиентами (Customers)

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-BILL-CUST-01 | `EnsureCustomer` | Unary | Upsert `Customer` по `user_id` и email; возврат `customer_id` и `provider`. |

---

# 3. Каталог SaaS-планов (Plans)

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-BILL-PLAN-01 | `ListPlans` | Unary | Каталог SaaS-планов с optional filter `only_active`; entitlements map для UI. |

---

# 4. Подписки SaaS (Subscriptions)

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-BILL-SUB-01 | `GetSubscription` | Unary | Effective subscription snapshot (Active/Trialing/PastDue в grace); пустой — UI fallback free. |
| SR-BILL-SUB-02 | `CreateCheckout` | Unary | Checkout session у payment provider; subscription `Incomplete` + redirect URL. |
| SR-BILL-SUB-03 | `CancelSubscription` | Unary | Отмена at period end или immediate. |

---

# 5. Access и entitlements

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-BILL-ACCESS-01 | `CheckAccess` | Unary | `has_access`, `plan_code`, `status`, `current_period_end` с grace для PastDue. |
| SR-BILL-ENT-01 | `GetEntitlements` | Unary | Map лимитов effective плана; fallback на default free plan. |

---

# 6. Инвойсы (Invoices)

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-BILL-INV-01 | `ListInvoices` | Unary | Пагинированная история платежей customer. |

---

# 7. Webhook-оркестрация (Webhooks)

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-BILL-WH-01 | `ProcessWebhook` | Unary | Verify signature, idempotency по (`Provider`, `EventId`), normalized ApplyEvents. |

---

# 8. Идентификация пользователя

Все RPC, кроме `ListPlans` и `ProcessWebhook`, принимают `user_id` как **string UUID** в теле request (не gRPC metadata). Aggregator маппит JWT `sub` → `user_id`. Невалидный GUID → `INVALID_ARGUMENT`.

Billing **не** валидирует JWT — доверяет caller (Aggregator, VocabularyService) внутри Docker network (h2c).

Детали RPC — в групповых файлах `01`–`06` и [[09 - Платформенные контракты (Operations)]].
