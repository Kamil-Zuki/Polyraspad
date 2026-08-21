# gRPC сообщения (billing.proto)

Protobuf messages для `BillingService` gRPC. Полный proto: [[../gRPC/billing.proto]].

# 1. Access & entitlements

| Message | Поля (ключевые) | RPC |
| :--- | :--- | :--- |
| `CheckAccessRequest` | `user_id` | CheckAccess |
| `CheckAccessResponse` | `has_access`, `plan_code`, `status`, `current_period_end` | |
| `GetEntitlementsRequest` | `user_id` | GetEntitlements |
| `GetEntitlementsResponse` | `plan_code`, `entitlements` map | |

# 2. Subscriptions & checkout

| Message | Поля (ключевые) | RPC |
| :--- | :--- | :--- |
| `GetSubscriptionRequest` | `user_id` | GetSubscription |
| `GetSubscriptionResponse` | `subscription` | |
| `ListPlansRequest` | `only_active` | ListPlans |
| `ListPlansResponse` | `plans[]` (`Plan`) | |
| `CreateCheckoutRequest` | `user_id`, `email`, `plan_code`, `provider`, `return_url` | CreateCheckout |
| `CreateCheckoutResponse` | `checkout_url`, `provider_payment_id` | |
| `CancelSubscriptionRequest` | `user_id`, `cancel_at_period_end` | CancelSubscription |
| `CancelSubscriptionResponse` | `subscription` | |

# 3. Invoices & customers

| Message | Поля | RPC |
| :--- | :--- | :--- |
| `ListInvoicesRequest` | `user_id`, `page`, `page_size` | ListInvoices |
| `ListInvoicesResponse` | `invoices[]` | |
| `EnsureCustomerRequest` | `user_id`, `email` | EnsureCustomer |
| `EnsureCustomerResponse` | `customer_id`, `provider` | |

# 4. Webhooks

| Message | Поля | RPC |
| :--- | :--- | :--- |
| `ProcessWebhookRequest` | `provider`, `payload`, `signature` | ProcessWebhook |
| `ProcessWebhookResponse` | `processed` | |

# 5. Shared entities

| Message | Назначение |
| :--- | :--- |
| `Plan` | Каталог SaaS-планов (`code`, `price`, `entitlements`, …) |
| `Subscription` | Статус подписки, period, trial, cancel flags |
| `Invoice` | История платежей |

REST mirror на Aggregator: [[Aggregator Service/04 - Бекенд, API и Контракты/Методы API/DTO/05 - Сообщество, биллинг и агент (Community Billing Agent)|Aggregator Billing DTO]].
