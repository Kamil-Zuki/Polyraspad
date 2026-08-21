# Введение

Группа алгоритмов **Webhook-оркестрация** применяет **нормализованные domain events** от payment adapters к PostgreSQL projection — subscriptions, invoices, payment_methods.

**SR:** **SR-BILL-WH-01**. КАР: [[02 - КАР-3 - Нормализованные webhook-события|КАР-3]]. gRPC entry: `#grpc-ProcessWebhook`.

---

# 1. Список алгоритмов

| Название алгоритма | SR | Краткое описание |
| :--- | :--- | :--- |
| **WebhookOrchestrator.ApplyEvents** | SR-BILL-WH-01 | Switch по DomainEvent type; transactional updates |

---

# Алгоритм WebhookOrchestrator.ApplyEvents

## Контекст и область применения

### Почему был создан

Raw webhook JSON провайдеров различается; домен работает с unified events (`PaymentSucceeded`, `PaymentFailed`, …). Orchestrator — единственная точка мутации subscription state после checkout.

### Бизнес-требование

**SR-BILL-WH-01** — idempotent apply после signature verify и insert в `processed_webhooks`.

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Первичная активация subscription после checkout |
| 2 | Renewal confirmation через inbound webhook |
| 3 | Сохранение payment method для RenewalWorker |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Duplicate event — skip apply (idempotency store) |
| 2 | Unknown subscription in event — log warning, no throw |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `events` | `IEnumerable<DomainEvent>` | Normalized list from adapter | Да |
| `PaymentSucceededEvent` | record | paymentId, customerId, amount, currency, paidAt, card meta | Условно |
| `PaymentFailedEvent` | record | paymentId, customerId | Условно |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| DB mutations | — | Updated subscriptions, invoices, payment_methods |
| Side effect | — | Logs для audit |

## Логика работы (Псевдокод)

```csharp
foreach (var evt in events)
{
    switch (evt)
    {
        case PaymentSucceededEvent ps:
            subscription = FindSubscription(ps); // by ProviderSubscriptionId or CustomerId
            subscription.Status = Active;
            subscription.CurrentPeriodStart = UtcNow;
            subscription.CurrentPeriodEnd = UtcNow.AddMonths(1);
            await InvoiceService.HandlePaymentSucceededAsync(ps);
            break;

        case PaymentFailedEvent pf:
            subscription.Status = PastDue;
            break;

        case SubscriptionUpdatedEvent su:
            // Sync status/period from provider-managed adapters (future Stripe)
            break;

        case PaymentMethodSavedEvent pm:
            Upsert payment_methods; IsDefault = true;
            break;
    }
}
await context.SaveChangesAsync();
```

## Связанные артефакты

* gRPC: `#grpc-ProcessWebhook`
* Entity: [[Entity - Инвойсы и webhook-идемпотентность - Invoices#Обработанный webhook (`processed_webhooks`)|processed_webhooks]]
* Entity: [[Entity - Подписки SaaS - Subscriptions|subscriptions]]
* КАР: [[02 - КАР-3 - Нормализованные webhook-события]]
