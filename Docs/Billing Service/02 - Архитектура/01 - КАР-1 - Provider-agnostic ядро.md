# Введение

Billing Service реализует **provider-agnostic ядро**: подписки, access-check, entitlements и webhook orchestration **не содержат** кода ЮKassa или Stripe. Все provider-specific операции изолированы в `IPaymentProvider` adapters.

## Контекст и проблема

Без абстракции каждый новый провайдер (Stripe, Paddle) требует правок `SubscriptionService`, `WebhookOrchestrator` и тестов. Смешение домена и HTTP-клиента провайдера повышает риск PCI и регрессий при смене API.

## Принятое решение

1. Интерфейс `IPaymentProvider` с checkout, recurring, status, webhook handle, signature verify.
2. Нормализованные DTO: `CheckoutSessionResult`, `DomainEvent` (`PaymentSucceeded`, …).
3. `PaymentProviderFactory` регистрирует адаптеры в DI (`Mock`, `YooKassa`).
4. `Billing:DefaultProvider` выбирает default; per-request override в `CreateCheckout`.
5. Доменные сервисы применяют только normalized events в `WebhookOrchestrator`.

## Обоснование и последствия

### Плюсы

* Stripe/Paddle — новый adapter class, без переписывания subscription lifecycle.
* Unit tests на orchestrator с `MockPaymentProvider`.
* Единый idempotency и invoice upsert для всех провайдеров.

### Последствия

* Двойной путь renewal (worker + webhook) для LocallyManaged — нужна idempotency на payment id.
* v1: один `Provider` на customer — documented limitation при смене провайдера.

SR: [[01 - Функциональная спецификация/Возможности сервиса/06 - Платёжные провайдеры (Payment Providers)#SR-BILL-PROV-01|SR-BILL-PROV-01]].
