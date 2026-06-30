---
name: steos-docs-04-coordinator
description: Plans and orchestrates filling folder 04 for a STEOS microservice — manifest, group alignment from 01, write order, delegation to writer/verifier. Use when generating entire 04, batch 04 work, or user invokes @docs-04-coordinator.
---

# STEOS Docs — 04 Coordinator

Orchestrates **folder `04 - Бекенд, API и Контракты`** for a target service. Does **not** duplicate block templates — those live in `.cursor/rules/steos-docs-folder-04-*.mdc`.

## Prerequisites

Before starting `04`:

1. Target service has stable **`03`** and **`01`** (group names and SR codes).
2. **`02`** КАР available for Rabbit/Redis/integrations alignment.
3. User named the service folder (e.g. `Messenger Service/`).

If upstream folders are missing → stop and tell user to complete `03 → 01 → 02` first.

## Workflow

1. Read [workflow.md](workflow.md).
2. Build **manifest** from target `01` groups + Auth folder tree — see [manifest-template.md](manifest-template.md).
3. Mark each file: `missing` | `stub` | `partial` | `done`.
4. Delegate writing in **write order** (see `steos-docs-04-write` → `write-order.md`):
   - Prefer `@docs-04-writer` per subfolder or group batch.
5. After each batch (or full pass) → `@docs-04-verifier` (readonly).
6. Return **coordinator report** (see below).

## gRPC-only services

Omit from manifest any subfolders not used: REST API, Socket, Redis, Integrations — per service architecture in `02`.

## Coordinator report template

```markdown
# Coordinator report — {ServiceName} / 04

## Manifest summary
- Total files: N | done: X | partial: Y | missing: Z

## Next write batch
1. …
2. …

## Blockers
- …

## Staging
- Open ISSUEs: N (see 99 - Staging)
```

## References

- Rules G2: `.cursor/rules/steos-docs-folder-04-coordinator.mdc`
- Etalon tree: `(Done) Authorization Service/04 - Бекенд, API и Контракты/`
- AGENTS.md — staging path
