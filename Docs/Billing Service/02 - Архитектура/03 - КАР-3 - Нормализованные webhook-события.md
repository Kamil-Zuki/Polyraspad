# Введение

Входящие webhooks платёжных провайдеров **нормализуются** в доменные события перед применением к PostgreSQL. `WebhookOrchestrator` обрабатывает только normalized types, не raw JSON.

## Контекст и проблема

Raw webhook payloads различаются по провайдерам. Прямой парсинг в orchestrator приводит к ветвлениям `if yookassa` / `if stripe` в core domain.

## Принятое решение

1. `IPaymentProvider.HandleWebhookAsync` → `WebhookHandleResult` с list `DomainEvent`.
2. Events v1: `PaymentSucceeded`, `PaymentFailed`, `SubscriptionUpdated`, `PaymentMethodSaved`.
3. `WebhookOrchestrator.ApplyEventsAsync` — единый switch по event type.
4. Ingress: Aggregator REST → gRPC `ProcessWebhook` (Billing не exposed).

## Обоснование и последствия

### Плюсы

* Тесты orchestrator без HTTP fixtures провайдера.
* Новый event type — extend enum + one handler method.

### Последствия

* Adapter must map all payment outcomes; unknown events logged and skipped.
* Idempotency на `processed_webhooks` до apply — duplicate safe.

SR: [[01 - Функциональная спецификация/Возможности сервиса/07 - Webhook-оркестрация (Webhooks)#SR-BILL-WH-01|SR-BILL-WH-01]].
