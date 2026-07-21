# Введение

Группа **Инвойсы и webhook-идемпотентность** фиксирует финансовые документы по подпискам и журнал уже обработанных webhook-событий провайдеров.

Провайдер — source of truth для факта оплаты; наша БД — **проекция** для UI истории платежей и доменной оркестрации.

---

# Инвойс (`invoices`)

## 1. Общее описание

**Invoice** — запис о платеже/счёте, связанная с `BillingSubscription`. Upsert из нормализованных webhook events через `InvoiceService`.

## 2. Атрибуты (поля) сущности

**Таблица:** `billing.Invoices`

| Название | Тип данных | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `Id` | `uuid` | PK | Внутренний ID инвойса. |
| `SubscriptionId` | `uuid` | FK, NOT NULL | Подписка, к которой относится платёж. |
| `Provider` | `enum` | NOT NULL | Провайдер платежа. |
| `ProviderInvoiceId` | `text` | NOT NULL | ID платежа/счёта в провайдере. |
| `AmountDue` | `int` | NOT NULL | Сумма к оплате (минимальные единицы). |
| `AmountPaid` | `int` | NOT NULL | Оплаченная сумма. |
| `Currency` | `text` | NOT NULL | Валюта. |
| `Status` | `enum` | NOT NULL | `Draft`, `Open`, `Paid`, `Uncollectible`, `Void`. |
| `InvoicePdfUrl` | `text` | NULL | Ссылка на PDF чека (если провайдер даёт). |
| `PaidAt` | `timestamp` | NULL | UTC успешной оплаты. |
| `CreatedAt` | `timestamp` | NOT NULL | UTC создания записи. |

*Индексы:* UNIQUE composite (`Provider`, `ProviderInvoiceId`).

## 3. Связи и RPC

| Consumer | Описание |
| :--- | :--- |
| `ListInvoices` gRPC | Пагинированный список для UI `/billing/invoices`. |
| **AggregatorService** | REST proxy `GET /api/billing/invoices`. |

---

# Обработанный webhook (`processed_webhooks`)

## 1. Общее описание

**ProcessedWebhook** — idempotency store для входящих webhook events. Composite PK (`Provider`, `EventId`) гарантирует at-most-once применение доменных событий.

## 2. Атрибуты (поля) сущности

**Таблица:** `billing.ProcessedWebhooks`

| Название | Тип данных | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `Provider` | `enum` | PK (part) | Провайдер события. |
| `EventId` | `text` | PK (part) | В **текущем коде** = SHA-256 hex всего `payload` (тот же string, что и `PayloadHash`). Не provider-native event id. |
| `EventType` | `text` | NOT NULL | Тип (`payment.succeeded`, …) после parse adapter. |
| `ProcessedAt` | `timestamp` | NOT NULL | UTC обработки. |
| `PayloadHash` | `text` | NOT NULL | В коде записывается **то же значение**, что `EventId` (`ComputePayloadHash`). |

> **Согласованность:** желаемая модель «EventId от провайдера + отдельный PayloadHash» зафиксирована в ISSUE-002; `03` здесь отражает **фактическое** поведение кода.

## 3. Алгоритм (код)

1. `ProcessWebhook` вычисляет `eventId = SHA256(payload)` (hex).
2. Lookup `ProcessedWebhooks` по (`Provider`, `EventId`) — при hit → already processed (no re-apply).
3. Provider adapter парсит payload → normalized `DomainEvent` list (signature verify — см. ISSUE-003).
4. INSERT `ProcessedWebhooks` с `EventId = PayloadHash = eventId`.
5. `WebhookOrchestrator.ApplyEventsAsync` обновляет `Subscriptions`, `Invoices`, `PaymentMethods`.

## 4. Нормализованные события (v1)

| Event | Действие |
| :--- | :--- |
| `PaymentSucceeded` | Activate/renew subscription; upsert invoice Paid. |
| `PaymentFailed` | `PastDue` или failed state. |
| `SubscriptionUpdated` | Sync status/period from provider. |
| `PaymentMethodSaved` | Upsert `payment_methods`, set default. |
