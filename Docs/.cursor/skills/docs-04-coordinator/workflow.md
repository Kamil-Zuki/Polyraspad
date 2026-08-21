# Coordinator workflow

## Phase 0 — Discovery

1. Resolve `<ServiceRoot>/` path.
2. List existing `04/**` files vs Auth etalon structure ([../docs-04-write/folder-tree.md](../docs-04-write/folder-tree.md)).
3. Read `<ServiceRoot>/01 - Функциональная спецификация/Возможности сервиса/00 - Общая информация.md` — extract **group names and order**.
4. Confirm `99 - Staging — Разрывы согласованности (DO NOT DELETE)/` exists; create from template if missing.

## Phase 1 — Manifest

For each expected file under `04/`:

| Column | Value |
| :--- | :--- |
| Path | relative to service root |
| Group | from `01` |
| Status | missing / stub / partial / done |
| Depends on | upstream SR, entities, КАР |

**Naming:** group file names must match `01` capability groups — never invent Auth group titles for a non-Auth service.

## Phase 2 — Write batches

Order (full stack):

1. `Методы API/gRPC/` (+ `.proto`) — source of truth
2. `Методы API/DTO/`
3. `Методы API/REST API/` — only if BFF exposes HTTP
4. `Методы API/Socket/` — only if WebSocket used
5. `Интеграции со сторонними сервисами/`
6. `Работа с Rabbit MQ/`
7. `Работа с Redis/`
8. `Алгоритмы и методы бекенда/`

Within each subfolder: `00 - … - Общая информация.md` first, then `01`, `02`, … by group order from `01`.

Invoke `@docs-04-writer` with explicit scope, e.g.:

> Write gRPC group 2 for `{Service}` — file `02 - ….md`, SR from `01` group 2.

## Phase 3 — Verify

After each batch or before marking 04 complete:

> `@docs-04-verifier` audit `{Service}/04` against `01`/`03`/`02`.

Do not delete or rename `99 - Staging`.

## Phase 4 — Report

Output coordinator report (see SKILL.md). List next files, open ISSUEs, blockers.
