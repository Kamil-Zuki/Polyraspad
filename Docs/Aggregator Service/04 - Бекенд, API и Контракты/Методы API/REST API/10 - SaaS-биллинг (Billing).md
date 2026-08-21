# Введение

SaaS billing — JWT для user routes; webhook — anonymous + optional `X-Billing-Webhook-Key`. Downstream: **BillingService** (`billing.proto`). Сверено с `AggregatorService/Controllers/BillingController.cs`.

**Примечание:** `Community.EntitlementDto` (deck marketplace) ≠ `Billing.EntitlementsDto` (SaaS plan).

DTO: [[../DTO/05 - Сообщество, биллинг и агент (Community Billing Agent)]].

# 1. Список эндпоинтов

| SR | Method | Route | gRPC | Auth |
| :--- | :--- | :--- | :--- | :--- |
| SR-AGG-BILL-01 | GET | `/api/Billing/access` | CheckAccess | JWT |
| SR-AGG-BILL-01 | GET | `/api/Billing/entitlements` | GetEntitlements | JWT |
| SR-AGG-BILL-01 | GET | `/api/Billing/subscription` | GetSubscription | JWT |
| SR-AGG-BILL-01 | GET | `/api/Billing/plans` | ListPlans | JWT |
| SR-AGG-BILL-01 | POST | `/api/Billing/checkout` | CreateCheckout | JWT |
| SR-AGG-BILL-01 | POST | `/api/Billing/subscription/cancel` | CancelSubscription | JWT |
| SR-AGG-BILL-01 | GET | `/api/Billing/invoices` | ListInvoices | JWT |
| SR-AGG-BILL-02 | POST | `/api/Billing/webhooks/{provider}` | ProcessWebhook | Webhook key |

---

# SR-AGG-BILL-01: Access snapshot: GET /api/Billing/access

## Общая информация

Badge и gating UI: paid vs free plan snapshot для текущего пользователя.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | `AccessDto` |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* JWT → `MappingHelper.GetUserId`
* gRPC [`CheckAccess`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/04%20-%20Access%20и%20entitlements.md#grpc-CheckAccess) на BillingService
* Map proto → `AccessDto`

## Успешный ответ

HTTP **200**, `AccessDto` (`hasAccess`, `planCode`, `status`, `currentPeriodEnd`).

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **401 Unauthorized** | Missing/invalid JWT |
| **502 Bad Gateway** | BillingService unavailable |

---

# SR-AGG-BILL-01: Entitlements: GET /api/Billing/entitlements

## Общая информация

Лимиты effective SaaS-плана для enforcement в VocabularyService и UI.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | `EntitlementsDto` |

## Логика обработки запроса

* JWT → user_id
* gRPC [`GetEntitlements`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/04%20-%20Access%20и%20entitlements.md#grpc-GetEntitlements)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-BILL-01: Subscription: GET /api/Billing/subscription

## Общая информация

Текущая подписка пользователя для billing dashboard.

| Тип метода | GET |
| :--- | :--- |
| **DTO успешного ответа** | `SubscriptionDto` (nullable fields если нет active row) |

## Логика обработки запроса

* JWT → user_id
* gRPC [`GetSubscription`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Подписки%20SaaS%20(Subscriptions).md#grpc-GetSubscription)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-BILL-01: Plans catalog: GET /api/Billing/plans

## Общая информация

Каталог SaaS-планов для pricing UI.

| Тип метода | GET |
| :--- | :--- |
| **Query** | `onlyActive` (bool, default `true`) |
| **DTO успешного ответа** | `List<PlanDto>` |

## Логика обработки запроса

* gRPC [`ListPlans`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/02%20-%20Каталог%20SaaS-планов%20(Plans).md#grpc-ListPlans) (JWT не требует user_id для catalog)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **502** | Downstream |

---

# SR-AGG-BILL-01: Checkout: POST /api/Billing/checkout

## Общая информация

Создание checkout session у платёжного провайдера.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `CheckoutRequestDto` (`planCode`, optional provider) |
| **DTO успешного ответа** | `CheckoutResponseDto` (`checkoutUrl`, subscription snapshot) |

## Логика обработки запроса

* JWT → user_id + email из claims
* gRPC [`CreateCheckout`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Подписки%20SaaS%20(Subscriptions).md#grpc-CreateCheckout) (внутри — `EnsureCustomer`)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Invalid plan / validation |
| **401** | JWT |
| **502** | BillingService / provider |

---

# SR-AGG-BILL-01: Cancel subscription: POST /api/Billing/subscription/cancel

## Общая информация

Отмена подписки at period end или immediate.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `CancelSubscriptionRequestDto` (`cancelAtPeriodEnd`) |
| **DTO успешного ответа** | `SubscriptionDto` |

## Логика обработки запроса

* JWT → user_id
* gRPC [`CancelSubscription`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Подписки%20SaaS%20(Subscriptions).md#grpc-CancelSubscription)
* Если downstream вернул null → **404** `{ error: "No active subscription found" }`

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | No active subscription |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-BILL-01: Invoices: GET /api/Billing/invoices

## Общая информация

Пагинированный список инвойсов пользователя.

| Тип метода | GET |
| :--- | :--- |
| **Query** | `page` (default 1), `pageSize` (default 20) |
| **DTO успешного ответа** | `List<InvoiceDto>` |

## Логика обработки запроса

* JWT → user_id
* gRPC [`ListInvoices`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/05%20-%20Инвойсы%20(Invoices).md#grpc-ListInvoices)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-BILL-02: Provider webhook: POST /api/Billing/webhooks/{provider}

## Общая информация

Прокси webhook от ЮKassa/mock в BillingService. **AllowAnonymous**; опциональная проверка `X-Billing-Webhook-Key`.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | Raw body (JSON string) |
| **DTO успешного ответа** | `{ "processed": true }` |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| `provider` | string | Имя провайдера (`yookassa`, `mock`, …) |

## Логика обработки запроса

* Если `Billing:WebhookApiKey` задан — сверка заголовка `X-Billing-Webhook-Key`
* Чтение raw body; signature из `X-Webhook-Signature` или `YooKassa-Signature`
* gRPC [`ProcessWebhook`](../../../../Billing%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/06%20-%20Webhook-оркестрация%20(Webhooks).md#grpc-ProcessWebhook)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | Invalid/missing webhook API key |
| **502** | BillingService error |
