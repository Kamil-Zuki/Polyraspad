# Введение

Billing Service **не открывает публичный REST** для браузера. Единственный внешний контракт микросервиса — **gRPC** на порту `5127`; пользовательский HTTP — **AggregatorService** BFF.

## Контекст и проблема

Прямой expose Billing увеличивает attack surface (webhooks, checkout) и дублирует JWT validation. Webhooks от провайдеров должны ingress на уже существующий API gateway.

## Принятое решение

1. Billing hosts `BillingService` gRPC service only (+ `/healthz`).
2. Aggregator `BillingController`: JWT routes + webhook proxy без JWT.
3. Optional `BILLING_WEBHOOK_API_KEY` на Aggregator ingress.
4. Vocabulary → direct gRPC `GetEntitlements` internal network.
5. Docker: `billing-service` expose 5127 только в `backend` network.

## Обоснование и последствия

### Плюсы

* Один JWT validation point (Aggregator).
* Nginx config не требует нового public route для Billing.
* Clear service boundary in AGENTS.md topology.

### Последствия

* Все REST DTO документируются в Aggregator `04/REST API`; Billing `04` — gRPC blocks.
* Latency +1 hop для user billing actions (acceptable for SaaS UI).

SR: [[01 - Функциональная спецификация/Возможности сервиса/09 - Платформенные контракты (Operations)#SR-BILL-OPS-01|SR-BILL-OPS-01]].

REST proxy: [[Aggregator Service/01 - Функциональная спецификация/Возможности сервиса/10 - SaaS-биллинг (Billing)]].
