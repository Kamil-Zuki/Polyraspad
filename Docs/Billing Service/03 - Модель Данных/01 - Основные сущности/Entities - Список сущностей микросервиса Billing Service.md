# Введение

Настоящий индекс описывает **персистентные сущности** микросервиса **Billing Service** — источник истины для SaaS-биллинга Polyraspad.

Данные хранятся в PostgreSQL, схема **`billing`**. Сервис **не** управляет подписками на колоды (deck subscriptions) — это домен `VocabularyService`.

## Группы сущностей

| Группа | Файл | Таблицы |
| :--- | :--- | :--- |
| Клиенты и платёжные методы | [[Entity - Клиенты и платёжные методы - Customers]] | `customers`, `payment_methods` |
| Каталог SaaS-планов | [[Entity - Каталог SaaS-планов - Plans]] | `plans`, `plan_provider_prices` |
| Подписки SaaS | [[Entity - Подписки SaaS - Subscriptions]] | `subscriptions` |
| Инвойсы и webhook-идемпотентность | [[Entity - Инвойсы и webhook-идемпотентность - Invoices]] | `invoices`, `processed_webhooks` |

## gRPC ↔ сущности

| RPC | Основные сущности |
| :--- | :--- |
| `EnsureCustomer` | `customers` |
| `ListPlans` | `plans` |
| `GetSubscription`, `CreateCheckout`, `CancelSubscription` | `customers`, `subscriptions`, `plans`, `payment_methods` |
| `CheckAccess`, `GetEntitlements` | `subscriptions`, `plans` (fallback `IsDefault`) |
| `ListInvoices` | `invoices` |
| `ProcessWebhook` | `processed_webhooks`, `subscriptions`, `invoices`, `payment_methods` |

## Seed планы (v1)

| Code | Price (коп.) | Trial | Entitlements (jsonb) |
| :--- | :--- | :--- | :--- |
| `free` | 0 | 0 | `maxProjects=3`, `maxCards=500`, `aiRequestsPerDay=10` |
| `pro` | 99000 | 7 | `maxProjects=50`, `maxCards=10000`, `aiRequestsPerDay=100` |
