# Введение

В данном документе описывается **межсервисное взаимодействие Billing Service** с **AggregatorService** (REST BFF, webhook ingress) и **VocabularyService** (enforcement SaaS-лимитов).

Оба consumer вызывают Billing по **gRPC h2c** на порту `5127` внутри Docker network. Billing **не** валидирует JWT — identity передаёт caller.

**SR:** **SR-AGG-BILL-01**, **SR-AGG-BILL-02**, **SR-BILL-ENT-01**, **SR-BILL-OPS-01**.

---

# Общая информация

| Параметр | Описание |
| :--- | :--- |
| **Протокол** | gRPC / Protocol Buffers (`billing.proto`, package `pvs.billing.v1`) |
| **Transport** | HTTP/2 cleartext (h2c); clients enable `Http2UnencryptedSupport` |
| **Порт Billing** | `5127` (`ASPNETCORE_URLS=http://0.0.0.0:5127`) |
| **Контракт** | [[../Методы API/gRPC/00 - gRPC - Общая информация|gRPC — Общая информация]] |

---

# AggregatorService (REST BFF → gRPC)

**Роль:** единственная публичная HTTP-точка для browser billing UI и webhook ingress.

## Доступ и аутентификация

| Параметр | Описание |
| :--- | :--- |
| **User REST** | JWT Bearer — `[Authorize]` на `BillingController`; `user_id` из claim, не из body |
| **Webhook REST** | `[AllowAnonymous]`; optional `X-Billing-Webhook-Key` если `Billing:WebhookApiKey` configured |
| **Email for checkout** | Claim `Email` / `JwtRegisteredClaimNames.Email` |

## Маппинг REST → gRPC

| REST (Aggregator) | gRPC | SR |
| :--- | :--- | :--- |
| `GET /api/Billing/access` | `#grpc-CheckAccess` | SR-AGG-BILL-01 |
| `GET /api/Billing/entitlements` | `#grpc-GetEntitlements` | SR-AGG-BILL-01 |
| `GET /api/Billing/subscription` | `#grpc-GetSubscription` | SR-AGG-BILL-01 |
| `GET /api/Billing/plans` | `#grpc-ListPlans` | SR-AGG-BILL-01 |
| `POST /api/Billing/checkout` | `#grpc-CreateCheckout` | SR-AGG-BILL-01 |
| `POST /api/Billing/subscription/cancel` | `#grpc-CancelSubscription` | SR-AGG-BILL-01 |
| `GET /api/Billing/invoices` | `#grpc-ListInvoices` | SR-AGG-BILL-01 |
| `POST /api/Billing/webhooks/{provider}` | `#grpc-ProcessWebhook` | SR-AGG-BILL-02 |

**EnsureCustomer:** вызывается неявно внутри checkout flow или при инициализации billing UI (`#grpc-EnsureCustomer`).

## Webhook ingress flow

1. ЮKassa POST на public nginx → Aggregator.
2. Aggregator validates `X-Billing-Webhook-Key` (if configured).
3. Читает raw body; forwards `provider`, `payload`, signature header (`X-Webhook-Signature` / `YooKassa-Signature`).
4. gRPC `ProcessWebhook` → Billing applies idempotent domain events.
5. HTTP 200 `{ "processed": true }` — провайдер прекращает retries.

---

# VocabularyService (gRPC client)

**Роль:** enforcement platform limits перед create project/card и AI requests.

## Ключевые вызовы

| gRPC | SR | Использование |
| :--- | :--- | :--- |
| `GetEntitlements` | SR-BILL-ENT-01 | Чтение `maxProjects`, `maxCards`, `aiRequestsPerDay` для текущего user |

**Fail-open (NFR-BILL-005):** при недоступности Billing VocabularyService использует free-tier limits, не блокируя пользователя полностью.

`CheckAccess` Vocabulary **не** вызывает в v1 — access gating на UI через Aggregator.

---

# Логика обработки запросов

* **Thin BFF:** Aggregator не хранит billing rows; каждый REST call — fresh gRPC to Billing.
* **Error mapping:** gRPC `NOT_FOUND` на cancel → HTTP 404; `INVALID_ARGUMENT` → 400.
* **No REST on Billing:** [[02 - КАР-5 - gRPC-only perimeter|КАР-5]] — Billing exposes only gRPC + `/healthz`.

---

# Обработка ошибок

| Тип ошибки | Caller | Реакция |
| :--- | :--- | :--- |
| **Billing unavailable** | Aggregator | HTTP 503 на REST; Vocabulary fail-open на free entitlements |
| **Invalid JWT** | Aggregator | HTTP 401 до вызова Billing |
| **Invalid webhook key** | Aggregator | HTTP 401; Billing not called |
| **gRPC DEADLINE_EXCEEDED** | Either | Retry policy на client; log warning |

---

*Публичный REST-контракт Aggregator: [[Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/10 - SaaS-биллинг (Billing)|REST API — Billing]].*
