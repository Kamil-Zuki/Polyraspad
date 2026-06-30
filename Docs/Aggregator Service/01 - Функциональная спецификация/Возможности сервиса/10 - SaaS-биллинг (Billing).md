# Группа 10: SaaS-биллинг (Billing)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **BillingService** — access, entitlements, subscription lifecycle, plans, checkout, invoices. Webhook endpoint для payment providers **без JWT**, с optional `X-Billing-Webhook-Key`.

Billing state (subscriptions, invoices, entitlements) живёт в BillingService PostgreSQL. Aggregator — typed gRPC client + webhook ingress на nginx public URL.

**Метафора:**

Представьте **кассу в lobby SaaS-приложения**. Пользователь с пропуском (JWT) спрашивает «какой у меня тариф?» и «оформить подписку»; платёжная система стучится в **служебную дверь** (webhook) с отдельным ключом, не через пользовательский JWT.

REST-контракты: [[04 - Бекенд, API и Контракты/Методы API/REST API/10 - SaaS-биллинг (Billing)|REST API — Billing]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к SaaS-биллингу.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-BILL-01** | **Self-service SaaS-биллинг:** Access, entitlements, subscription, checkout, cancel и invoices для JWT-пользователя через BillingService. |
| **SR-AGG-BILL-02** | **Приём payment webhooks:** Inbound события от payment provider с optional shared key; пользовательский JWT не участвует. |

---

# Детальная спецификация требований

## SR-AGG-BILL-01: Access, entitlements, subscription (JWT) {#SR-AGG-BILL-01}

Пользовательские billing operations — `BillingController` под `[Authorize]`, кроме webhook action.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **userId from JWT** | `MappingHelper.GetUserId` — не из body. |
| **Email for checkout** | Claim `Email` / `JwtRegisteredClaimNames.Email` для provider session. |
| **gRPC BillingService** | `IBillingServiceClient` — port 5127. |
| **Read-heavy** | access, entitlements, subscription, plans, invoices — GET. |
| **Mutations** | checkout, cancel subscription — POST. |
| **404 on cancel** | No active subscription → HTTP **404**. |

### 2. Высокоуровневое описание

Представим billing UI flow как **личный кабинет абонента**.

1. **Access/Entitlements:** frontend gate features (Reader limits, marketplace premium) — быстрые read checks при app load.
2. **Subscription/Plans:** текущий plan status + catalog для upgrade page.
3. **Checkout:** создаёт payment session у provider (mock dev / YooKassa prod) — redirect URL в response.
4. **Cancel/Invoices:** self-service cancel at period end; paginated invoice history.

Aggregator не хранит billing rows — каждый call fresh gRPC to BillingService.

Таким образом, **monetization truth** centralized в Billing; Aggregator — REST ergonomics + webhook ingress.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `BillingController`, base `/api/Billing`.
* **JWT:** Bearer на все actions кроме webhook.

#### Сценарий А: Check access before Reader feature (Happy Path)

**Сценарий:** UI проверяет, доступен ли premium Reader import.

1. **GET** `/api/Billing/access` + Bearer.
2. **gRPC:** `CheckAccessAsync(userId)`.
3. **Ответ:** HTTP **200**, `AccessDto`.

#### Сценарий Б: Checkout upgrade (Happy Path)

1. **POST** `/api/Billing/checkout`, body `CheckoutRequestDto` (planId).
2. **BFF:** userId + email from claims → `CreateCheckoutAsync`.
3. **Ответ:** HTTP **200**, checkout URL/session DTO.

#### Сценарий В: List invoices (Happy Path)

1. **GET** `/api/Billing/invoices?page=1&pageSize=20`.
2. **Ответ:** HTTP **200**, `List<InvoiceDto>`.

#### Сценарий Г: Cancel without subscription (Negative Path)

1. **POST** `/api/Billing/subscription/cancel`.
2. **Billing:** null subscription.
3. **Ответ:** HTTP **404** `{ "error": "No active subscription found" }`.

---

## SR-AGG-BILL-02: Webhook proxy {#SR-AGG-BILL-02}

Inbound webhooks от payment provider → `BillingService.ProcessWebhook`. `[AllowAnonymous]` на action; trust via optional shared key + provider signature downstream.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Optional API key** | If `Billing:WebhookApiKey` set — header `X-Billing-Webhook-Key` must match. |
| **Raw body** | Full payload read from request stream. |
| **Signature forward** | `X-Webhook-Signature` or `YooKassa-Signature` → BillingService. |
| **Provider route** | `{provider}` path segment (yookassa, mock). |
| **Idempotent downstream** | Billing handles duplicate webhook delivery. |

### 2. Высокоуровневое описание

Представим webhook как **почтовый слот для банка**.

1. **Provider** POST payment.succeeded event на public Aggregator URL (via nginx).
2. **Aggregator** validates webhook API key if configured.
3. **Forwards** raw JSON + signature headers to BillingService gRPC.
4. **Billing** updates subscription/invoice state idempotently.
5. **Response** 200 `{ processed: true }` — provider stops retries.

User JWT **не участвует** — separate trust boundary.

Таким образом, Aggregator — **webhook demilitarized zone** между internet и internal Billing gRPC.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** POST `/api/Billing/webhooks/{provider}`.
* **Auth:** AllowAnonymous + optional `X-Billing-Webhook-Key`.

#### Сценарий А: Valid webhook (Happy Path)

**Сценарий:** YooKassa notifies payment success.

1. **POST** body + signature headers.
2. **Key check (if configured):** pass.
3. **gRPC:** `ProcessWebhookAsync(provider, payload, signature)`.
4. **Ответ:** HTTP **200** `{ "processed": true }`.

#### Сценарий Б: Invalid webhook key (Negative Path)

1. **Key configured;** header missing or wrong.
2. **Ответ:** HTTP **401** `{ "error": "Invalid webhook API key" }`; Billing not called.

---

*Следующая группа: [[11 - AI-агент (Agent)]].*
