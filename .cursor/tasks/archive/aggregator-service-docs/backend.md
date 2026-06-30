# Backend Task — STEOS Docs for Aggregator Service

Plan ID: `aggregator-service-docs`
Agent: `backend-agent`
Status: done
Can run in parallel: no

## Objective

Write complete STEOS microservice documentation for AggregatorService at:
`Docs/Aggregator Service/`

Follow rules in `Docs/.cursor/rules/` and layout from `(Done) Authorization Service/` (format only — NO Auth domain content).

## Inputs

- Plan: `.cursor/plans/active/aggregator-service-docs_b7e4a2f1.plan.md`
- Template: `Docs/Шаблон документации микросервиса STEOS/`
- Etalon layout: `Docs/(Done) Authorization Service/`
- Code: `AggregatorService/` (all Controllers, Services, Dtos, Protos, Program.cs)
- Write rules: `Docs/.cursor/skills/steos-docs-04-write/SKILL.md`, `write-order.md`
- Core rules: `Docs/.cursor/rules/steos-docs-core.mdc`, `steos-docs-folders-010305.mdc`

## Scope

### Folder structure

```
Docs/Aggregator Service/
├── README.md
├── 01 - Функциональная спецификация/
│   ├── Термины и определения.md
│   ├── Нефункциональные требования.md
│   └── Возможности сервиса/
│       ├── 00 - Общая информация.md
│       └── NN - {Group}.md (one per capability group)
├── 02 - Архитектура/
│   ├── 00 - Общая архитектура и структура.md
│   └── NN - КАР-N - {Name}.md
├── 03 - Модель Данных/
│   └── 01 - Основные сущности/
│       ├── Entities - Список сущностей.md
│       └── Entity - {Domain}.md (API contract views, NOT DB — BFF is stateless)
├── 04 - Бекенд, API и Контракты/
│   ├── README.md
│   ├── Методы API/
│   │   ├── DTO/ (grouped by 01 groups)
│   │   └── REST API/ (all controllers)
│   ├── Интеграции со сторонними сервисами/ (downstream gRPC + external HTTP)
│   └── Алгоритмы и методы бекенда/ (JWT validation, AI proxy, TTS, doc extraction, rate limit)
└── 99 - Staging — Разрывы согласованности (DO NOT DELETE)/
    ├── 00 - Реестр проблем.md
    └── ISSUE-001-primer-problemy.md (example stub)
```

**SKIP for Aggregator:** `04/.../gRPC/` (no gRPC server), `Socket/`, `Rabbit MQ/`, `Redis/`

### 01 Groups (SR-AGG-*)

Derive from controllers:

1. Аутентификация и профиль (AuthController) — SR-AGG-AUTH-*
2. Проекты и колоды (Projects, Decks) — SR-AGG-CONTENT-*
3. Карточки и редактор (CardsController) — SR-AGG-CARD-*
4. Сессии обучения (StudyController) — SR-AGG-STUDY-*
5. Аналитика (AnalyticsController) — SR-AGG-ANALYTICS-*
6. Reader и термины (Terms, Text) — SR-AGG-READER-*
7. Подписки на колоды (SubscriptionsController) — SR-AGG-SUB-*
8. Сообщество и маркетплейс (CommunityController) — SR-AGG-COMM-*
9. Медиа и Reader Library (MediaController) — SR-AGG-MEDIA-*
10. SaaS-биллинг (BillingController) — SR-AGG-BILL-*
11. AI-агент (AgentController) — SR-AGG-AGENT-*
12. AI-прокси (AiProxyController) — SR-AGG-AI-*
13. Автоматизация и эксперименты (AutomationController) — SR-AGG-AUTO-*
14. Внешние интеграции (IntegrationController) — SR-AGG-INT-*
15. Настройки пользователя (UserSettingsController) — SR-AGG-SETTINGS-*
16. Платформенные контракты (healthz, CORS, rate limit, prod validation) — SR-AGG-OPS-*

### 02 KAR (minimum 5)

- КАР-1: Thin BFF — REST-to-gRPC маршрутизация
- КАР-2: Локальная валидация JWT
- КАР-3: Двойная модель аутентификации (JWT + Proxy Keys)
- КАР-4: HTTP-прокси медиа (обход CORS)
- КАР-5: Rate Limiting публичной аутентификации
- КАР-6: Production Configuration Guard

### 03 for stateless BFF

Document **API Contract Entities** — logical groupings of DTO fields at gateway boundary. Note explicitly: persistent data owned by downstream services. Link fields to DTO files in 04.

### 04 REST

Document ALL controllers with endpoint blocks per `steos-docs-folder-04-rest-api.mdc`. Link each endpoint to downstream gRPC method name (not full gRPC doc — Aggregator doesn't host gRPC).

## Out of Scope

- Code changes in AggregatorService/
- Copying Authorization Service domain text
- Folder 05

## Deliverables

All markdown files listed above with real content from code (routes, DTOs, gRPC clients, config).

## Verification

- Grep SR-AGG codes appear in 01 and 04
- README links to main sections
- No `SR-AUTH-*` codes in Aggregator docs

## Handoff

Return: files created list, SR group count, any ISSUEs filed, blockers.
