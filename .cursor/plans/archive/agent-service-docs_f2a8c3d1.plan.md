---
name: agent-service-docs
overview: Написать полную STEOS-документацию для микросервиса Agent Service (03→01→02→04) по шаблону и правилам Docs/, на основе реального кода в AgentService/.
todos:
  - id: scaffold-docs-structure
    content: Создать структуру Docs/Agent Service/ (01, 02, 03, 04, 99, README) по шаблону STEOS.
    status: completed
  - id: write-03-entities
    content: Написать 03 — сущности AgentThread, AgentMessage, AgentRun, AgentToolCall, AgentDomainDecision, AgentArtifact.
    status: completed
  - id: write-01-sr-groups
    content: Написать 01 — 00, термины, NFR и SR-группы с таблицами «Название и Описание» (формат Aggregator).
    status: completed
  - id: write-02-kar
    content: Написать 02 — общая архитектура и КАР (gRPC-only, orchestration, domain policy, LLM).
    status: completed
  - id: write-04-contracts
    content: Написать 04 — gRPC, DTO, Integrations, Algorithms; пропустить REST/Redis/RabbitMQ/Socket.
    status: completed
  - id: verify-docs
    content: Проверить 01↔03, staging ISSUE при расхождениях, reviewer pass.
    status: completed
isProject: false
---

# Agent Service — STEOS Documentation

## Goal

Создать авторитетную документацию микросервиса **Agent Service** в `Docs/Agent Service/` по структуре `(Done) Authorization Service/` (layout only) и правилам `Docs/.cursor/rules/`.

AgentService — gRPC-only микросервис (.NET 10, port 5131): AI assistant threads, message history, orchestrated runs с intent routing, domain policy, LLM tools и интеграцией с VocabularyService. PostgreSQL schema `internal`. Без REST, Redis, RabbitMQ.

## Out of Scope

- Изменения кода AgentService
- Папка `05 - Сводная документация`
- REST API docs (публичный REST — Aggregator → gRPC Agent)
- Frontend agent UI (polyraspad-frontend)

## Agents

- `backend-agent`: основной writer — анализ кода + markdown в Docs/
- `product-agent`: не нужен
- `frontend-agent`: не нужен
- `reviewer-agent`: проверка после implementation slice

## Contracts To Lock

- SR prefix: `SR-AGENT-*`
- 01 groups aligned with gRPC domains + orchestration layers
- 03 = PostgreSQL entities in schema `internal`
- 04 gRPC methods map 1:1 to agent.proto; each links algorithms/integrations
- Omit: REST API folder, Redis, RabbitMQ, Socket
- Table column: `Название и Описание` with `**Bold title:** description` pattern (Aggregator etalon)

## Tasks

- `.cursor/tasks/active/agent-service-docs/backend.md`
- `.cursor/tasks/active/agent-service-docs/review.md`

## Verification

- All 01 SR codes grounded in 03 entities
- No Auth domain text copied
- Staging folder with registry
- Spot-check agent.proto vs gRPC docs

## Cleanup

- [ ] Subagents launched and handoffs collected
- [ ] Frontmatter todos — completed or cancelled
- [ ] tasks/active → archive
- [ ] plans/active → archive
