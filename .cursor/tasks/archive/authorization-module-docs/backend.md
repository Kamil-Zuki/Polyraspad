# Backend Task — STEOS Docs for Authorization Module

Plan ID: `authorization-module-docs`
Agent: `backend-agent`
Status: done
Can run in parallel: no

## Objective

Write complete STEOS microservice documentation for `authorization-module` at:
`Docs/Authorization Module/`

Follow `Docs/.cursor/rules/` and layout from `(Done) Authorization Service/` (**format only**).

## Inputs

- Plan: `.cursor/plans/active/authorization-module-docs_f2a8c3d1.plan.md`
- Code: `authorization-module/authorization-module.API/`
- Etalon format: `Docs/Aggregator Service/01 - Функциональная спецификация/Возможности сервиса/07 - Подписки на колоды (Deck Subscriptions).md`
- Table column: `| Код | Название и Описание |` with `**Title:** description` pattern

## Scope

### 01 Groups (4 groups, SR-AUTHMOD-*)

1. Регистрация и подтверждение email — SR-AUTHMOD-REG-01..02
2. Аутентификация и JWT-токены — SR-AUTHMOD-AUTH-01..04
3. Управление профилем — SR-AUTHMOD-PROF-01..05
4. Платформенные контракты — SR-AUTHMOD-OPS-01..04

Each group file: intro + metaphor + SR table + detailed SR blocks (§1/§2/§3).

### 03 Entities

- ApplicationUser (IdentityUser + AvatarUrl)
- RefreshToken

### 02 KAR

- КАР-1: ASP.NET Core Identity + PostgreSQL
- КАР-2: JWT Access + Refresh Token Rotation
- КАР-3: gRPC-first API с REST legacy

### 04

- gRPC: all AuthService RPCs from authorization.proto
- DTO: request/response records
- REST: AccountsController legacy routes
- Integrations: SMTP email
- Algorithms: TokenService, email confirmation flow

## Out of Scope

- Copy STEOS Auth domain text
- Invent Redis/RabbitMQ/OIDC features

## Verification

- grep SR-AUTHMOD in 01 and 04
- No SR-AUTH-SM or Phantom Token references

## Handoff

Return files created, SR count, ISSUEs filed, blockers.
