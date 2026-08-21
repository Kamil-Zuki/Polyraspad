# Группа 6: Платёжные провайдеры (Payment Providers)

## Введение

В этом разделе описывается **абстракция платёжных провайдеров** — replaceable adapters поверх provider-agnostic домена.

Доменные сервисы вызывают `IPaymentProvider` через factory; ЮKassa и Mock — первые реализации. Stripe — future adapter без изменения `BillingSubscription` lifecycle.

**Метафора:**

Представьте **универсальный POS-терминал с сменными модулями оплаты**. Касса (домен Billing) всегда пробивает один и тот же чек подписки; в слот можно вставить модуль ЮKassa, Mock для тестов или в будущем Stripe — без переписывания учётной книги.

Архитектура: [[02 - КАР-1 - Provider-agnostic ядро]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к платёжным провайдерам.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-PROV-01** | **Provider abstraction:** IPaymentProvider, factory и normalized checkout/recurring/webhook DTO — домен не парсит raw JSON провайдера. |
| **SR-BILL-PROV-02** | **ЮKassa adapter:** HTTP API, Basic Auth, idempotence key, webhook mapping; режим LocallyManaged. |
| **SR-BILL-PROV-03** | **Mock provider:** Dev/tests без credentials; фиктивный checkout URL и programmatic success. |

---

# Детальная спецификация требований

## SR-BILL-PROV-01: Provider abstraction {#SR-BILL-PROV-01}

Единый контракт `IPaymentProvider` изолирует SubscriptionService, WebhookOrchestrator и RenewalWorker от конкретного payment API.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **IPaymentProvider** | Checkout, recurring, payment status, webhook handle, signature verify. |
| **Factory** | `GetProvider(code)` / `GetDefaultProvider()` из DI registry. |
| **Normalized results** | CheckoutSessionResult, RecurringPaymentResult, WebhookHandleResult + DomainEvents. |
| **Config** | `Billing:DefaultProvider`; dev fallback `mock` без YooKassa keys. |

### 2. Высокоуровневое описание

Представим provider abstraction как **универсальный POS-терминал со сменными модулями оплаты**.

1. **Checkout:** SubscriptionService вызывает `CreateCheckoutAsync` на выбранном provider через factory.
2. **Webhook:** adapter парсит raw payload → нормализованный список `DomainEvent`.
3. **Orchestrator:** `WebhookOrchestrator` применяет events без знания формата ЮKassa, Mock или future Stripe.

Таким образом, добавление нового провайдера — новый class + DI registration, не правки lifecycle подписки.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Checkout через default provider (Happy Path)

1. **CreateCheckout** с `provider` empty → factory `GetDefaultProvider()`.
2. **Adapter** возвращает `checkout_url` и `provider_payment_id`.
3. **DB:** subscription `Incomplete` до webhook.

---

## SR-BILL-PROV-02: ЮKassa adapter {#SR-BILL-PROV-02}

Первая production adapter для РФ: one-time checkout с `save_payment_method` и recurring payments для LocallyManaged подписок.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **LocallyManaged** | Подписки ЮKassa — наш renewal worker, не provider subscription object. |
| **save_payment_method** | Checkout сохраняет карту для recurring token. |
| **Sandbox** | `UseSandbox` config для тестов. |
| **Webhook map** | `payment.succeeded` → PaymentSucceeded + PaymentMethodSaved. |

### 2. Высокоуровневое описание

Представим ЮKassa adapter как **модуль оплаты в слот POS-терминала для российского эквайринга**.

1. **Checkout HTTP:** typed HttpClient с Basic Auth (`ShopId:SecretKey`), header `Idempotence-Key` на create payment.
2. **Сохранение карты:** POST payments с `capture=true`, `save_payment_method=true` → `confirmation_url` для redirect.
3. **Return URL:** после оплаты browser возвращается на `PaymentProviders:YooKassa:ReturnUrl`; активация — через webhook.
4. **Recurring:** `RenewalWorker` вызывает `CreateRecurringPaymentAsync` с `payment_method_id` из сохранённой карты.

Таким образом, ЮKassa — transport layer в режиме `LocallyManaged`; статусы подписки обновляет только normalized event pipeline.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Успешный checkout redirect (Happy Path)

1. **CreateCheckoutAsync** → POST payments с `capture=true`, `save_payment_method=true`.
2. **Ответ:** `confirmation_url` для browser redirect.
3. **После оплаты:** webhook `payment.succeeded` активирует subscription.

#### Сценарий Б: Recurring charge (Happy Path)

1. **RenewalWorker** вызывает `CreateRecurringPaymentAsync` с `payment_method_id`.
2. **Ответ provider:** status succeeded → subscription period extended.

---

## SR-BILL-PROV-03: Mock provider {#SR-BILL-PROV-03}

Dev/test adapter без внешних credentials — docker-compose default `BILLING_DEFAULT_PROVIDER=mock`.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Local dev default** | Автоматический mock при пустых YooKassa credentials + log warning. |
| **Tests** | Integration tests эмулируют success без external HTTP. |
| **No PCI** | Нет реальных платежей и card data. |

### 2. Высокоуровневое описание

Представим Mock provider как **тренажёр кассы без реального эквайринга**.

1. **Dev default:** docker-compose с `BILLING_DEFAULT_PROVIDER=mock` — checkout без YooKassa credentials.
2. **Фиктивный checkout:** `CreateCheckoutAsync` возвращает localhost URL; browser redirect имитирует оплату.
3. **Programmatic success:** integration tests вызывают `ProcessWebhook` с success payload для полного lifecycle.
4. **Без PCI:** нет реальных платежей, card data и внешних HTTP-зависимостей в CI.

Таким образом, локальный стек и CI проходят billing flows без секретов провайдера.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Local docker checkout (Happy Path)

1. **docker-compose:** `BILLING_DEFAULT_PROVIDER=mock`.
2. **POST checkout** → redirect URL на localhost success page.
3. **Test webhook** или manual ProcessWebhook → subscription Active.

---

*Следующая группа: [[07 - Webhook-оркестрация (Webhooks)]].*
