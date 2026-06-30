---
name: steos-docs-04-write
description: Writes markdown in folder 04 for STEOS microservices following Auth etalon structure, 01 group alignment, and Cursor rules G2/G3 block templates. Use when filling 04 files, invoked by coordinator or @docs-04-writer.
---

# STEOS Docs — 04 Writer

Writes content in `<Service>/04 - Бекенд, API и Контракты/`. **Block structure** comes from `.cursor/rules/` — do not invent alternate templates.

## Before writing

1. Confirm scope: single file, one group file, or one subfolder.
2. Read target service **`01`** (SR codes, group names) and **`03`** (entities/fields).
3. Read matching Auth etalon file for **depth and layout only** — never copy Auth domain data.
4. Read [write-order.md](write-order.md) if scope spans multiple subfolders.

## Rules map (G3)

| Subfolder | Rule file |
| :--- | :--- |
| DTO | `steos-docs-folder-04-dto.mdc` |
| gRPC | `steos-docs-folder-04-grpc.mdc` |
| REST API | `steos-docs-folder-04-rest-api.mdc` |
| Socket | `steos-docs-folder-04-socket.mdc` |
| Integrations | `steos-docs-folder-04-integrations.mdc` |
| Rabbit MQ | `steos-docs-folder-04-rabbitmq.mdc` |
| Redis | `steos-docs-folder-04-redis.mdc` |
| Algorithms | `steos-docs-folder-04-algorithms.mdc` |

G2 coordinator: `steos-docs-folder-04-coordinator.mdc` — layers, alignment, consistency.

## Writing rules

- Output **pure Markdown** only (no meta-commentary).
- Every table row in `00` and group files includes **SR from `01`**.
- gRPC first: anchors `#grpc-MethodName` before REST/Socket reference them.
- REST/Socket: **mandatory** link to delegated gRPC method.
- DTO/proto fields trace to **`03`** entities.
- Rabbit/Redis patterns align with **`02`** КАР.

## On ambiguity

Do not guess. Add ISSUE to `99 - Staging` using [issue-template.md](../steos-docs-04-verify/issue-template.md) and [steos-docs-staging-issues.mdc](../../rules/steos-docs-staging-issues.mdc).

## References

- [write-order.md](write-order.md)
- [folder-tree.md](folder-tree.md)
- `(Done) Authorization Service/04 - Бекенд, API и Контракты/`
