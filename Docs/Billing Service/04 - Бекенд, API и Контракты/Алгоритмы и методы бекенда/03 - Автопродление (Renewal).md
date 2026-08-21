# Введение

Группа алгоритмов **Автопродление** реализует фоновый **RenewalWorker** — proactive recurring charge для подписок в режиме **LocallyManaged** (ЮKassa v1).

Provider-managed подписки (future Stripe) worker **не** используют — renewal только через webhooks.

**SR:** **SR-BILL-REN-01**. КАР: [[02 - КАР-2 - LocallyManaged vs ProviderManaged|КАР-2]].

---

# 1. Список алгоритмов

| Название алгоритма | SR | Краткое описание |
| :--- | :--- | :--- |
| **RenewalWorker poll loop** | SR-BILL-REN-01 | IHostedService; recurring charge + PastDue grace cutoff |

---

# Алгоритм RenewalWorker poll loop

## Контекст и область применения

### Почему был создан

ЮKassa v1 не управляет subscription object платформы — период и статус в нашей БД. Worker списывает оплату до `current_period_end` по saved payment method.

### Бизнес-требование

**SR-BILL-REN-01** — poll interval, recurring charge, PastDue grace cutoff, skip при `CancelAtPeriodEnd`.

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | LocallyManaged Active/Trialing subscriptions с period end в ближайший час |
| 2 | PastDue subscriptions внутри grace window — retry charge |
| 3 | PastDue после grace — transition to Canceled |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Требуется default `payment_methods` row; иначе skip + log warning |
| 2 | `CancelAtPeriodEnd = true` — worker не продлевает |
| 3 | ProviderManaged mode — worker игнорирует |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `RenewalPollIntervalMinutes` | `int` | Config, default 15 | Да |
| `GracePeriodDays` | `int` | Config, default 3 | Да |
| `subscription` | `BillingSubscription` | LocallyManaged candidate | Да |
| `defaultPaymentMethod` | `PaymentMethod` | IsDefault = true | Да (для charge) |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `subscription.Status` | enum | Active (success) или PastDue (fail) |
| `CurrentPeriodEnd` | timestamp | +1 month on success |
| Logs | — | Warning/error для ops |

## Логика работы (Псевдокод)

```csharp
while (!stoppingToken.IsCancelled)
{
    using var scope = CreateScope();
    var now = UtcNow;
    var graceCutoff = now.AddDays(-GracePeriodDays);
    var renewalWindow = now.AddHours(1);

    // 1. Grace cutoff: PastDue + period_end < graceCutoff → Canceled
    foreach (var sub in PastDueExpired(graceCutoff))
        sub.Status = Canceled;

    // 2. Candidates: LocallyManaged, !CancelAtPeriodEnd
    var candidates = Subscriptions.Where(
        ManagementMode == LocallyManaged &&
        ((Active|Trialing) && PeriodEnd <= renewalWindow ||
         PastDue && PeriodEnd >= graceCutoff));

    foreach (var sub in candidates)
    {
        var pm = DefaultPaymentMethod(sub.CustomerId);
        if (pm == null) { LogWarning; continue; }

        var result = await provider.CreateRecurringPaymentAsync(...);
        if (result.Status == "succeeded")
        {
            sub.Status = Active;
            sub.CurrentPeriodStart = now;
            sub.CurrentPeriodEnd = now.AddMonths(1);
        }
        else
            sub.Status = PastDue;

        await SaveChanges();
    }

    await Delay(RenewalPollIntervalMinutes);
}
```

## Связанные артефакты

* Provider: [[01 - Платёжные провайдеры (Payment Providers)#Алгоритм ЮKassa HTTP adapter]]
* Entity: [[Entity - Клиенты и платёжные методы - Customers#Платёжный метод (`payment_methods`)|payment_methods]]
* Entity: [[Entity - Подписки SaaS - Subscriptions|subscriptions]]
* КАР: [[02 - КАР-2 - LocallyManaged vs ProviderManaged]]
