# Группа 7: Webhook-оркестрация (Webhooks)

## Введение

В этом разделе описывается **обработка inbound payment webhooks** — ingress через Aggregator REST, apply в Billing через gRPC `ProcessWebhook`.

Idempotency и normalized events — [[02 - КАР-3 - Нормализованные webhook-события]]. Сущность: [[Entity - Инвойсы и webhook-идемпотентность - Invoices#Обработанный webhook (`processed_webhooks`)|processed_webhooks]].

**Метафора:**

Представьте **почтовое отделение с журналом входящих**. Каждое письмо (webhook) получает штамп «уже обработано»; кассир (WebhookOrchestrator) обновляет абонемент только один раз.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к webhook-оркестрации.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-WH-01** | **Process webhook:** Verify signature, idempotency insert, provider parse, ApplyEvents. |

---

# Детальная спецификация требований

## SR-BILL-WH-01: Process webhook {#SR-BILL-WH-01}

Inbound payment events от провайдера применяются к подпискам и инвойсам **атомарно и один раз** — даже при retry доставки webhook.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Ingress path** | POST `/api/billing/webhooks/{provider}` на Aggregator → gRPC forward. |
| **Idempotency** | PK (`Provider`, `EventId`); duplicate → processed=true без re-apply. |
| **WebhookOrchestrator** | PaymentSucceeded → Active + period extend; PaymentFailed → PastDue. |
| **InvoiceService** | Upsert invoice on payment events. |

### 2. Высокоуровневое описание

Представим webhook pipeline как **почтовое отделение с журналом входящих писем**.

1. **Ingress:** Aggregator forwards raw payload + signature с `POST /api/billing/webhooks/{provider}`.
2. **Verify:** provider adapter вызывает `VerifyWebhookSignature` — невалидная подпись → reject.
3. **Idempotency:** INSERT `processed_webhooks`; при duplicate PK (`Provider`, `EventId`) — skip re-apply.
4. **Parse:** `HandleWebhookAsync` нормализует payload → список `DomainEvent`.
5. **Apply:** `ApplyEventsAsync` transactional обновляет subscription, invoice и payment method.

Таким образом, webhook path — единственный источник активации paid subscription после checkout (наряду с renewal worker для recurring).

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: payment.succeeded (Happy Path)

1. **Webhook:** ЮKassa POST → Aggregator → `ProcessWebhook`.
2. **Events:** PaymentSucceeded.
3. **DB:** subscription Active, period +1 month, invoice Paid.

#### Сценарий Б: Duplicate event (Happy Path idempotent)

1. **DB:** conflict on `processed_webhooks`.
2. **Ответ:** `processed=true` без изменения subscription.

---

*Следующая группа: [[08 - Автопродление (Renewal)]].*
