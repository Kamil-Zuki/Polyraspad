---
name: authorization-module-docs
overview: Написать полную STEOS-документацию для микросервиса authorization-module в Docs/Authorization Module/ по эталону формата и правилам Docs/.cursor/, с опорой на реальный код (Identity, JWT, gRPC, PostgreSQL).
todos:
  - id: folder-03-data-model
    content: "03 — Модель данных: ApplicationUser, RefreshToken, Entities index."
    status: completed
  - id: folder-01-functional
    content: "01 — Функциональная спецификация: 00 + 4 группы SR-AUTHMOD-* с таблицами «Название и Описание»."
    status: completed
  - id: folder-02-architecture
    content: "02 — Архитектура: 00 + КАР-1..3 (Identity, JWT rotation, gRPC-first)."
    status: completed
  - id: folder-04-contracts
    content: "04 — gRPC, DTO, REST legacy, SMTP integration, algorithms."
    status: completed
  - id: verify-and-staging
    content: "99 Staging реестр, README, reviewer pass."
    status: completed
isProject: false
---

# Authorization Module — STEOS Documentation

## Goal

Создать `Docs/Authorization Module/` — документацию Polyraspad identity-сервиса (`authorization-module/`), **не** копируя домен STEOS `(Done) Authorization Service` (Phantom Token, OIDC, Redis и т.д.).

## Out of Scope

- Изменения кода в `authorization-module/`
- Папка `05 - Сводная документация`
- Переписывание `(Done) Authorization Service/`
- Redis, RabbitMQ, WebSocket (не реализованы в коде)

## Agents

- `backend-agent`: написание docs из кода (03→01→02→04)
- `reviewer-agent`: readonly audit vs code и STEOS rules

## Contracts To Lock

- SR prefix: `SR-AUTHMOD-*`
- 4 capability groups (не 16 — сервис проще Aggregator)
- gRPC `Pvs.Auth.Grpc.AuthService` — primary API; REST `/api/v1/auth` — legacy/direct
- Entities: `ApplicationUser`, `RefreshToken` (PostgreSQL via EF Core Identity)

## Tasks

- `.cursor/tasks/active/authorization-module-docs/backend.md`
- `.cursor/tasks/active/authorization-module-docs/review.md`

## Verification

- Нет `SR-AUTH-SM-*` / Phantom Token / STEOS ID в тексте
- SR codes согласованы между 01 и 04
- Таблицы используют колонку `Название и Описание` как в Aggregator etalon
