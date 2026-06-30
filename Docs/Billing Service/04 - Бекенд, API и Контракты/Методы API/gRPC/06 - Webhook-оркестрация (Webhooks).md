# Введение

Методы данной группы обрабатывают **inbound payment webhooks** — ingress через Aggregator REST, apply в Billing через gRPC `ProcessWebhook`.

Idempotency — SHA256 hash payload как `EventId` в `processed_webhooks`. Normalized events — [[02 - КАР-3 - Нормализованные webhook-события|КАР-3]].

**SR группы:** **SR-BILL-WH-01**. Сущность: [[Entity - Инвойсы и webhook-идемпотентность - Invoices#Обработанный webhook (`processed_webhooks`)|processed_webhooks]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-BILL-WH-01 | `ProcessWebhook` | Unary | Parse provider payload, idempotency, `WebhookOrchestrator.ApplyEventsAsync`. |

---

<span id="grpc-ProcessWebhook"></span>

# SR-BILL-WH-01: Process webhook: ProcessWebhook

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/07 - Webhook-оркестрация (Webhooks)#SR-BILL-WH-01]]

Inbound payment events от провайдера применяются к подпискам и инвойсам **атомарно и один раз**. Aggregator может проверять `X-Billing-Webhook-Key`; verify provider signature в v1 — на adapter, но **не вызывается** из `BillingGrpcService` (см. ISSUE-003).

**REST-паритет:** `POST /api/Billing/webhooks/{provider}` — raw body + signature forwarded.

| Сигнатура | `rpc ProcessWebhook(ProcessWebhookRequest) returns (ProcessWebhookResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ProcessWebhookRequest` — `provider` (`yookassa` \| `mock` \| …), `payload` (raw JSON string), `signature` |
| **Сообщение ответа** | `ProcessWebhookResponse` — `processed` (bool; `true` при success и при duplicate) |

## Логика обработки запроса

1. Получить `IPaymentProvider` через `PaymentProviderFactory.GetProvider(provider)`; неизвестный code → `NotSupportedException`.
2. Вычислить `eventId = SHA256(payload)` (hex string) — idempotency key.
3. SELECT `processed_webhooks` BY (`Provider`, `EventId`); если запись есть — вернуть `{ processed: true }` без re-apply.
4. Собрать `WebhookPayload(body, signature, eventId)`.
5. `IPaymentProvider.HandleWebhookAsync(payload)` → список normalized `DomainEvent`.
6. `WebhookOrchestrator.ApplyEventsAsync(events)` — transactional update subscriptions, invoices, payment methods.
7. INSERT `ProcessedWebhook` (`Provider`, `EventId`, `EventType`, `ProcessedAt`, `PayloadHash`).
8. `SaveChangesAsync`; вернуть `{ processed: true }`.

**Нормализованные события (v1):**

| Event | Действие |
| :--- | :--- |
| `PaymentSucceeded` | Subscription → Active; extend period; upsert invoice Paid |
| `PaymentFailed` | Subscription → PastDue |
| `SubscriptionUpdated` | Sync status/period from provider |
| `PaymentMethodSaved` | Upsert `payment_methods`, set IsDefault |

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INTERNAL** | Неподдерживаемый `provider`; ошибка parse/orchestrator/PostgreSQL. |

> **Примечание:** `VerifyWebhookSignature` реализован на `IPaymentProvider`, но в `BillingGrpcService.ProcessWebhook` не вызывается до `HandleWebhookAsync`. SR-BILL-WH-01 требует verify signature — см. ISSUE-003.

---

*Документация gRPC-групп Billing Service завершена. См. [[00 - gRPC - Общая информация]] для сводных таблиц.*
