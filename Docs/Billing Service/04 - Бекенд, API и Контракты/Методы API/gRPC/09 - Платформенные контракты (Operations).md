# Введение

Группа **Operations** — эксплуатационные контракты Billing Service без дополнительных RPC в `billing.proto`. Единственный публичный API — gRPC `BillingService` + anonymous health.

# 1. Контракты (не-RPC)

| Код требования | Контракт | Описание |
| :------------- | :------- | :------- |
| SR-BILL-OPS-01 | gRPC server `:5127` | HTTP/2 only; `BillingGrpcService` maps all 9 RPC |
| SR-BILL-OPS-01 | `GET /healthz` | Anonymous liveness `{ "status": "ok" }` на том же Kestrel |
| SR-BILL-OPS-01 | No public REST | Browser REST только через Aggregator BFF |

---

# SR-BILL-OPS-01: gRPC server и healthz {#SR-BILL-OPS-01}

## Общая информация

**Источник требования:** [[../../../01 - Функциональная спецификация/Возможности сервиса/09 - Платформенные контракты (Operations)#SR-BILL-OPS-01]]

| Компонент | Значение |
| :--- | :--- |
| **Порт** | `5127` (Docker: `billing-service:5127`) |
| **Протокол** | gRPC over h2c |
| **Health** | `Program.cs` → `MapGet("/healthz", …)` |
| **Migrations** | EF Core at startup (Docker compose) |

## Callers

| Сервис | RPC usage |
| :--- | :--- |
| AggregatorService | CheckAccess, GetEntitlements, subscription, checkout, invoices, ProcessWebhook |
| VocabularyService | GetEntitlements, CheckAccess (limits enforcement) |

## Логика (ops)

1. Kestrel listens HTTP/2 only on 5127.
2. gRPC reflection enabled in Development.
3. Health endpoint не проверяет PostgreSQL deep check — process liveness only.

## Связанные артефакты

* Proto: [[billing.proto]]
* Aggregator REST: `REST API/10 - SaaS-биллинг (Billing)`
* КАР: `02 - Архитектура` Billing Service
