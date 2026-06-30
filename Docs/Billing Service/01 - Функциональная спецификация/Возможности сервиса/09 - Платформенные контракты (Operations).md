# Группа 9: Платформенные контракты (Operations)

## Введение

В этом разделе описываются **эксплуатационные контракты** Billing Service: gRPC server, health check и отсутствие публичного REST на самом микросервисе.

Пользовательский HTTP — только через AggregatorService ([[Aggregator Service/01 - Функциональная спецификация/Возможности сервиса/10 - SaaS-биллинг (Billing)|SR-AGG-BILL-*]]).

**Метафора:**

Представьте **внутренний отдел биллинга без витрины на улице**. Клиенты общаются с reception (Aggregator REST); другие отделы звонят по внутреннему номеру (gRPC 5127).

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к платформенным контрактам.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-OPS-01** | **gRPC и healthz:** Kestrel HTTP/2 на 5127; `GET /healthz` liveness; без REST controllers на Billing. |

---

# Детальная спецификация требований

## SR-BILL-OPS-01: gRPC и healthz {#SR-BILL-OPS-01}

Billing Service экспонирует только gRPC API и liveness probe — не участвует в JWT validation и не принимает browser traffic напрямую.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **gRPC only external API** | `billing.proto` service `BillingService` — 9 RPC methods. |
| **h2c internal** | Plaintext HTTP/2 в Docker network; clients enable Http2UnencryptedSupport. |
| **healthz** | Process liveness; не deep check PostgreSQL в v1. |
| **Port 5127** | `ASPNETCORE_URLS=http://0.0.0.0:5127` в compose. |
| **EF migrations** | Applied at startup (`Database.Migrate()`). |

### 2. Высокоуровневое описание

Представим Billing Service как **внутренний отдел биллинга без витрины на улице**.

1. **gRPC only:** Kestrel на `5127` экспонирует `billing.proto` — 9 RPC methods; REST controllers на Billing отсутствуют.
2. **Internal clients:** только `aggregator-service` и `vocabulary-service` вызывают Billing по h2c в Docker network.
3. **Healthz:** `GET /healthz` — liveness probe для compose и CI; deep check PostgreSQL в v1 не выполняется.
4. **Startup:** EF `Database.Migrate()` применяет pending migrations до приёма gRPC traffic.

Таким образом, security perimeter для SaaS billing совпадает с Aggregator + optional webhook key — Billing остаётся в private network.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Docker health (Happy Path)

1. **Request:** GET `http://billing-service:5127/healthz` from compose network.
2. **Ответ:** 200 OK — container healthy.

#### Сценарий Б: Vocabulary entitlement read (Happy Path)

1. **gRPC:** vocabulary-service → `GetEntitlements(user_id)` on `billing-service:5127`.

---

*Документация групп 01 завершена. Следующий шаг STEOS workflow: `04 - Бекенд, API и Контракты` (gRPC blocks).*
