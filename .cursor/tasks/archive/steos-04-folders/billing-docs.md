# Task: Billing Service — folder 04

**Status:** in_progress  
**Plan:** `steos-04-folders`

## Goal

Write complete `Docs/Billing Service/04 - Бекенд, API и Контракты/` from code + 01/02/03.

## Rules

- `Docs/.cursor/rules/steos-docs-folder-04-*.mdc`
- Etalon layout: `Docs/(Done) Authorization Service/04 - Бекенд, API и Контракты/`
- gRPC-only: skip REST, Socket, Redis, RabbitMQ

## Sources

- `BillingService/Protos/billing.proto` (or tests reference)
- `BillingService.Tests/` for behavior
- `Docs/Billing Service/01`, `02`, `03`
- Aggregator REST mapping: `Docs/Aggregator Service/04/.../10 - SaaS-биллинг`

## Deliverables

gRPC/00 + billing.proto + group files 01-07 aligned with 01 groups; Integrations 00, 01 YooKassa, 02 internal gRPC; Algorithms 00 + group-aligned files; update README.

## Verification

Each RPC has `#grpc-*` anchor; SR codes from 01; fields trace to 03.
