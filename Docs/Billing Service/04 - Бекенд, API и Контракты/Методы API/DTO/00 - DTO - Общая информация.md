# Введение

Billing Service — **gRPC-only** микросервис. Публичные REST DTO живут на **Aggregator** ([[Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/10 - SaaS-биллинг (Billing)|Aggregator REST — Billing]]).

В этой папке описаны **protobuf messages** из `billing.proto` — контракт данных между BillingService и клиентами (Aggregator, VocabularyService).

# 1. Группы DTO (protobuf)

| Группа | Файл | RPC |
| :--- | :--- | :--- |
| Access & entitlements | [[01 - gRPC сообщения (billing.proto)]] | CheckAccess, GetEntitlements |
| Subscriptions & checkout | [[01 - gRPC сообщения (billing.proto)]] | GetSubscription, ListPlans, CreateCheckout, CancelSubscription |
| Invoices & customers | [[01 - gRPC сообщения (billing.proto)]] | ListInvoices, EnsureCustomer |
| Webhooks | [[01 - gRPC сообщения (billing.proto)]] | ProcessWebhook |

Source of truth: `BillingService/Protos/billing.proto` (копия в `Docs/.../gRPC/billing.proto`).
