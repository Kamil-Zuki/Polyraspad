# Task: Billing Service — folder 04 gRPC batch

**Plan:** steos-04-billing-media  
**Status:** in_progress

## Goal

Write `Docs/Billing Service/04 - Бекенд, API и Контракты/` gRPC layer per `steos-docs-folder-04-grpc.mdc` and Auth Module gRPC etalon.

## Files to create

1. `Методы API/gRPC/00 - gRPC - Общая информация.md`
2. `Методы API/gRPC/billing.proto` (copy from BillingService/Protos/)
3. `01 - Управление клиентами (Customers).md` — EnsureCustomer
4. `02 - Каталог SaaS-планов (Plans).md` — ListPlans
5. `03 - Подписки SaaS (Subscriptions).md` — GetSubscription, CreateCheckout, CancelSubscription
6. `04 - Access и entitlements.md` — CheckAccess, GetEntitlements
7. `05 - Инвойсы (Invoices).md` — ListInvoices
8. `06 - Webhook-оркестрация (Webhooks).md` — ProcessWebhook

## Source code

- `BillingService/Protos/billing.proto`
- `BillingService/Grpc/BillingGrpcService.cs`
- `Docs/Billing Service/01/` SR codes SR-BILL-*

## Skip

REST API, DTO (REST on Aggregator), Redis, RabbitMQ, Socket
