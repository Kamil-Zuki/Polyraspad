# Введение

В данном документе описывается **HTTP/REST интеграция с ЮKassa** — первым production payment adapter для SaaS-биллинга Polyraspad в РФ.

Adapter реализует `IPaymentProvider` в режиме **LocallyManaged** ([[02 - КАР-2 - LocallyManaged vs ProviderManaged|КАР-2]]): renewal подписок выполняет `RenewalWorker`, а не объект subscription у провайдера.

**SR:** **SR-BILL-PROV-02**. Связанные gRPC: `#grpc-CreateCheckout`, `#grpc-ProcessWebhook`.

---

# Общая информация

| Параметр | Описание |
| :--- | :--- |
| **Версия API** | YooKassa API v3 (`https://api.yookassa.ru/v3/`) |
| **Название сервиса** | ЮKassa (YooMoney) |
| **Владелец интеграции** | Команда Billing / Backend Polyraspad |
| **Sandbox** | `PaymentProviders:YooKassa:UseSandbox` — тестовый shop |

---

# Доступ и аутентификация

| Параметр | Описание |
| :--- | :--- |
| **Метод аутентификации** | HTTP Basic Auth — Base64(`ShopId:SecretKey`) |
| **Хранение учётных данных** | Environment / K8s Secrets: `YOOKASSA_SHOP_ID`, `YOOKASSA_SECRET_KEY`; **never frontend** |
| **Webhook secret** | `PaymentProviders:YooKassa:WebhookSecret` — HMAC-SHA256 verify входящих событий |
| **Среды** | Prod — live shop; Dev/Stage — sandbox или fallback на Mock при пустых credentials |

---

# Ключевые методы HTTP/REST

| Метод | Endpoint | SR | Использование в Billing Service |
| :--- | :--- | :--- | :--- |
| **POST** | `/v3/payments` | SR-BILL-PROV-02 | `CreateCheckoutAsync` — redirect checkout с `save_payment_method=true`, `capture=true` |
| **POST** | `/v3/payments` | SR-BILL-PROV-02 | `CreateRecurringPaymentAsync` — autopayment с `payment_method_id` (RenewalWorker) |
| **GET** | `/v3/payments/{id}` | SR-BILL-PROV-02 | `GetPaymentStatusAsync` — опрос статуса платежа |
| **Inbound webhook** | Aggregator `POST /api/Billing/webhooks/yookassa` | SR-BILL-WH-01 | Raw JSON → `#grpc-ProcessWebhook` → `HandleWebhookAsync` |

**Idempotence-Key:** каждый POST payments получает уникальный header `Idempotence-Key` (GUID) для безопасных retries.

**Checkout metadata:** `planCode`, `customerId` — для correlation в webhook → subscription activation.

---

# Логика обработки запросов

* **Timeout:** typed `HttpClient` с разумным timeout (30s default ASP.NET).
* **Retry:** transient HTTP errors — Polly retry на caller side (recommended for production).
* **Amount format:** сумма в рублях string `"9.90"` из копеек entity (`price / 100`).
* **Return URL:** `PaymentProviders:YooKassa:ReturnUrl` или `CreateCheckoutRequest.return_url` — redirect после оплаты на `/billing/success`.
* **Webhook mapping (v1):**

| YooKassa event | Domain events |
| :--- | :--- |
| `payment.succeeded` | `PaymentSucceeded` + optional `PaymentMethodSaved` |
| `payment.canceled` | `PaymentFailed` |
| `refund.succeeded` | out of scope v1 |

---

# Обработка ошибок

| Тип ошибки | Причина | Реакция сервиса |
| :--- | :--- | :--- |
| **401 Unauthorized** | Неверный ShopId/SecretKey | Log error; checkout/recurring fail → gRPC INTERNAL / FAILED_PRECONDITION |
| **400 Bad Request** | Невалидное тело payment | Log response body; propagate to caller |
| **Invalid webhook signature** | Подделка или wrong secret | `#grpc-ProcessWebhook` → PERMISSION_DENIED |
| **Duplicate webhook** | Retry доставки провайдера | Idempotency `processed_webhooks` → `{ processed: true }` без re-apply |

---

*См. также: [[02 - Внутренние микросервисы (Aggregator, Vocabulary)]] для ingress path webhooks.*
