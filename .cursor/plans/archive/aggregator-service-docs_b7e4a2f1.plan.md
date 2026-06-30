---
name: aggregator-service-docs
overview: Написать полную STEOS-документацию для микросервиса AggregatorService (BFF/API Gateway) по шаблону и правилам Docs/, на основе реального кода в AggregatorService/.
todos:
  - id: scaffold-docs-structure
    content: Создать структуру папок Docs/Aggregator Service/ (01, 02, 03, 04, 99, README) по шаблону STEOS.
    status: completed
  - id: write-03-01-02
    content: Написать 03 (контракты BFF), 01 (SR-группы), 02 (КАР) на основе кода AggregatorService.
    status: completed
  - id: write-04-rest-integrations
    content: Написать 04 — REST API, DTO, Integrations, Algorithms; пропустить gRPC-server/Redis/RabbitMQ.
    status: completed
  - id: verify-docs
    content: Проверить согласованность 01↔03↔04, staging ISSUE при расхождениях, reviewer pass.
    status: completed
isProject: false
---

# Aggregator Service — STEOS Documentation

## Goal

Создать авторитетную документацию микросервиса **Aggregator Service** в `Docs/Aggregator Service/` по структуре `(Done) Authorization Service/` и правилам `Docs/.cursor/rules/`.

AggregatorService — stateless BFF (.NET 10): REST наружу, gRPC к authorization-module, VocabularyService, MediaService, AgentService, BillingService. Без собственной БД, Redis, RabbitMQ.

## Out of Scope

- Изменения кода AggregatorService
- Папка `05 - Сводная документация` (только после полного 04, не в этом slice)
- Документирование gRPC-серверов downstream-сервисов (только ссылки/интеграции)
- Frontend API client docs (polyraspad-frontend)

## Agents

- `backend-agent`: основной writer — анализ кода + markdown в Docs/
- `product-agent`: не нужен (SR выводятся из кода и домена Polyraspad)
- `frontend-agent`: не нужен
- `reviewer-agent`: проверка после implementation slice

## Contracts To Lock

- SR prefix: `SR-AGG-*` (не AUTH)
- 01 groups aligned with controller domains
- 03 = API boundary contracts (stateless BFF, no owned DB entities)
- 04 REST endpoints map 1:1 to controllers; each endpoint links downstream gRPC
- Omit: gRPC server folder, Redis, RabbitMQ, Socket (no WebSocket on Aggregator)

## Tasks

- `.cursor/tasks/active/aggregator-service-docs/backend.md`
- `.cursor/tasks/active/aggregator-service-docs/review.md`

## Verification

- All 01 SR codes referenced in 04 REST tables
- No Auth domain text copied (layout only from etalon)
- Staging folder exists with registry
- Spot-check 3 controllers vs REST docs

## Cleanup

- [ ] Subagents launched and handoffs collected
- [ ] Frontmatter todos — completed or cancelled
- [ ] tasks/active → archive
- [ ] plans/active → archive
