# Введение

Группа алгоритмов **Платёжные провайдеры** изолирует домен Billing Service от конкретных payment API (ЮKassa, Mock, future Stripe).

Реализует **SR-BILL-PROV-01**, **SR-BILL-PROV-02**, **SR-BILL-PROV-03**. Архитектура: [[02 - КАР-1 - Provider-agnostic ядро|КАР-1]].

---

# 1. Список алгоритмов

| Название алгоритма | SR | Краткое описание |
| :--- | :--- | :--- |
| **Provider abstraction (IPaymentProvider + factory)** | SR-BILL-PROV-01 | Checkout, recurring, webhook handle, signature verify |
| **ЮKassa HTTP adapter** | SR-BILL-PROV-02 | POST /v3/payments, webhook event mapping |
| **Mock provider** | SR-BILL-PROV-03 | Dev default без credentials |

---

# Алгоритм Provider abstraction (IPaymentProvider + factory)

## Контекст и область применения

### Почему был создан

Домен подписок и webhook orchestration не должен знать формат JSON ЮKassa или Stripe. Смена провайдера — новый adapter class + DI registration, без правок `BillingSubscription` lifecycle.

### Бизнес-требование

**SR-BILL-PROV-01** — единый контракт `IPaymentProvider` + factory по config `Billing:DefaultProvider`.

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | `#grpc-CreateCheckout` — `CreateCheckoutAsync` |
| 2 | `RenewalWorker` — `CreateRecurringPaymentAsync` |
| 3 | `#grpc-ProcessWebhook` — `HandleWebhookAsync`, `VerifyWebhookSignature` |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Один активный `Provider` на customer в v1 |
| 2 | Неподдерживаемый provider code → `NotSupportedException` |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `providerCode` | `string` | `mock`, `yookassa`, `stripe` | Нет (default из config) |
| `CheckoutRequest` | record | userId, email, planCode, customerId, price, currency, returnUrl | Да (checkout) |
| `WebhookPayload` | record | body, signature, eventId | Да (webhook) |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `CheckoutSessionResult` | record | confirmationUrl, providerPaymentId |
| `RecurringPaymentResult` | record | providerPaymentId, status, paidAt |
| `WebhookHandleResult` | record | `IReadOnlyList<DomainEvent>` |

## Логика работы (Псевдокод)

```csharp
// PaymentProviderFactory.GetProvider(code)
var provider = string.IsNullOrWhiteSpace(code)
    ? _providers[_options.DefaultProvider]
    : _providers[code]; // NotSupportedException if missing

// CreateCheckoutAsync — вызывается из SubscriptionService
var checkout = await provider.CreateCheckoutAsync(request, ct);
// Subscription Incomplete уже в БД; ProviderSubscriptionId обновляется после ответа
```

## Связанные артефакты

* gRPC: `#grpc-CreateCheckout`, `#grpc-ProcessWebhook`
* Интеграция: [[../Интеграции со сторонними сервисами/01 - ЮKassa (HTTP)]]
* КАР: [[02 - КАР-1 - Provider-agnostic ядро]]

---

# Алгоритм ЮKassa HTTP adapter

## Контекст и область применения

### Почему был создан

Production payments в РФ через ЮKassa с сохранением карты для LocallyManaged renewal.

### Бизнес-требование

**SR-BILL-PROV-02** — checkout с `save_payment_method`, recurring autopayment, webhook mapping.

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Upgrade to Pro checkout redirect |
| 2 | Monthly renewal via `RenewalWorker` |
| 3 | Webhook `payment.succeeded` activation |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Требуются `ShopId` + `SecretKey`; иначе startup fallback на Mock |
| 2 | `refund.succeeded` — out of scope v1 |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `ShopId`, `SecretKey` | `string` | Basic Auth credentials | Да (prod) |
| `amount` | `int` | Копейки | Да |
| `payment_method_id` | `string` | Saved PM token | Да (recurring) |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `confirmation_url` | `string` | Browser redirect (checkout) |
| `payment.id` | `string` | Provider payment id |
| `DomainEvent[]` | events | Из webhook JSON |

## Логика работы (Псевдокод)

```csharp
// Checkout
POST /v3/payments
  amount, capture=true, save_payment_method=true
  confirmation.type=redirect, return_url
  metadata: { planCode, customerId }
  Header: Idempotence-Key = Guid

// Recurring (RenewalWorker)
POST /v3/payments
  payment_method_id, amount, capture=true

// Webhook mapping
switch (event) {
  "payment.succeeded" => PaymentSucceeded + PaymentMethodSaved
  "payment.canceled" => PaymentFailed
}
```

## Связанные артефакты

* gRPC: `#grpc-CreateCheckout`, `#grpc-ProcessWebhook`
* КАР: [[02 - КАР-2 - LocallyManaged vs ProviderManaged]]

---

# Алгоритм Mock provider

## Контекст и область применения

### Почему был создан

Local docker-compose и CI без PCI credentials и external HTTP.

### Бизнес-требование

**SR-BILL-PROV-03** — `BILLING_DEFAULT_PROVIDER=mock` в dev.

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Integration tests full billing lifecycle |
| 2 | Local checkout redirect на localhost success page |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Нет реальных платежей и card data |

## Логика работы (Псевдокод)

```csharp
CreateCheckoutAsync => CheckoutSessionResult(localhostUrl, mockPaymentId)
HandleWebhookAsync => parse test payload => PaymentSucceeded events
VerifyWebhookSignature => true (no secret in dev)
```

## Связанные артефакты

* gRPC: `#grpc-CreateCheckout`, `#grpc-ProcessWebhook`
