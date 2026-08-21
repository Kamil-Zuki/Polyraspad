# 04 — Бекенд, API и Контракты

Папка `04` для **Billing Service** описывает gRPC-only контракт, интеграции и серверные алгоритмы SaaS-биллинга Polyraspad.

## Статус

| Подпапка | Статус |
| :--- | :--- |
| `Методы API/gRPC/` | **Done** — 9 RPC, `billing.proto`, группы `01`–`06`, `09` Operations |
| `Методы API/DTO/` | **Done** — protobuf messages (`00`, `01`) |
| `Методы API/REST API/` | **Skip** — [[Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/10 - SaaS-биллинг (Billing)|Aggregator REST — Billing]] |
| `Методы API/Socket/` | **N/A** |
| `Работа с Redis/` | **N/A** |
| `Работа с Rabbit MQ/` | **N/A** |
| `Интеграции/` | **Done** — ЮKassa HTTP, Aggregator/Vocabulary gRPC |
| `Алгоритмы и методы бекенда/` | **Done** — Providers (гр.06), Webhook (гр.07), Renewal (гр.08), Access projection |

## Соответствие групп `01` и `04`

| Группа `01` | gRPC `04` | Алгоритмы |
| :--- | :--- | :--- |
| 01 Customers | `01` EnsureCustomer | — |
| 02 Plans | `02` ListPlans | — |
| 03 Subscriptions | `03` GetSubscription, CreateCheckout, Cancel | — |
| 04 Access | `04` CheckAccess, GetEntitlements | `04` Access projection |
| 05 Invoices | `05` ListInvoices | — |
| 06 Payment Providers | — (adapter layer) | `01` Payment Providers |
| 07 Webhooks | `06` ProcessWebhook | `02` Webhook orchestration |
| 08 Renewal | — (background worker) | `03` Renewal |
| 09 Operations | `09` healthz / gRPC server | — |

## Дерево

```
04 - Бекенд, API и Контракты/
├── Методы API/gRPC/
│   ├── 00 - gRPC - Общая информация.md
│   ├── billing.proto
│   ├── 01 - Управление клиентами (Customers).md
│   ├── 02 - Каталог SaaS-планов (Plans).md
│   ├── 03 - Подписки SaaS (Subscriptions).md
│   ├── 04 - Access и entitlements.md
│   ├── 05 - Инвойсы (Invoices).md
│   ├── 06 - Webhook-оркестрация (Webhooks).md
│   └── 09 - Платформенные контракты (Operations).md
├── Методы API/DTO/
│   ├── 00 - DTO - Общая информация.md
│   └── 01 - gRPC сообщения (billing.proto).md
├── Интеграции со сторонними сервисами/
│   ├── 00 - … - Общая информация.md
│   ├── 01 - ЮKassa (HTTP).md
│   └── 02 - Внутренние микросервисы (Aggregator, Vocabulary).md
└── Алгоритмы и методы бекенда/
    ├── 00 - … - Общая информация.md
    ├── 01 - Платёжные провайдеры (Payment Providers).md
    ├── 02 - Webhook-оркестрация (Webhooks).md
    ├── 03 - Автопродление (Renewal).md
    └── 04 - Access и Entitlements projection.md
```

## Source of truth (код)

- `BillingService/Protos/billing.proto`
- `BillingService/Grpc/BillingGrpcService.cs`
- `BillingService/Services/` — Access, Entitlement, Subscription, Invoice, WebhookOrchestrator, RenewalWorker
- `BillingService/Providers/` — IPaymentProvider, YooKassa, Mock

## Staging (04 ↔ код)

| ISSUE | Тема |
| :--- | :--- |
| [[ISSUE-002-processwebhook-eventid-hash]] | EventId = hash payload vs provider id |
| [[ISSUE-003-processwebhook-no-signature-verify]] | Missing VerifyWebhookSignature in gRPC handler |
| [[ISSUE-004-getsubscription-effective-row]] | GetSubscription vs FindEffectiveSubscription |

Реестр: [[99 - Staging — Разрывы согласованности (DO NOT DELETE)/00 - Реестр проблем]].

## Upstream

- `01` — 9 capability groups, 14 SR codes
- `02` — КАР-1…КАР-5
- `03` — entities: customers, plans, subscriptions, invoices, processed_webhooks
