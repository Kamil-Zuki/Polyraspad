# Введение

SaaS subscriptions и entitlements. Proto: `billing.proto`. Config: `AggregatorService:BillingServiceBaseUrl`.

# Общая информация

| Параметр | Значение |
| :--- | :--- |
| **SR** | SR-AGG-BILL-* |
| **Webhook** | Provider → Aggregator POST /api/Billing/webhooks/* (API key) |

# gRPC методы

| REST | gRPC |
| :--- | :--- |
| GET access / entitlements | GetAccess, GetEntitlements |
| GET/POST subscription, checkout, cancel | GetSubscription, CreateCheckout, CancelSubscription |
| GET invoices | ListInvoices |

# Webhook proxy

Aggregator валидирует `BILLING_WEBHOOK_API_KEY`, forwards payload в BillingService gRPC/HTTP handler.

# Vocabulary entitlements

VocabularyService также вызывает BillingService напрямую для feature gates — Aggregator дублирует read-only access для UI.
