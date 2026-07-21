---
description: "[G3 · 04 · gRPC] RPC block template, proto, якоря #grpc-*"
globs: "**/04 - Бекенд, API и Контракты/**/Методы API/gRPC/**"
alwaysApply: false
---

# gRPC (`Методы API/gRPC/`)

Primary microservice contract. REST/Socket on Gateway map to these RPCs.

## `00 - gRPC - Общая информация.md`

1. `# Введение` — role of gRPC, Gateway callers, Zero Trust context.
2. `# 1. Группы методов gRPC` — summary table.
3. `# 2 … N` — per-group tables: SR | gRPC Method | RPC type | Description.

## Group file `NN - [Group name].md`

1. `# Введение` — scope and SR from `01`.
2. `# 1. Список методов` — RPC table before detail blocks.
3. Each RPC — block below; separate with `---`.

## RPC Block Template

```markdown
<span id="grpc-[MethodName]"></span>

# [SR-CODE]: [Название]: [MethodName]

## Общая информация

**Источник требования:** wikilink to SR in `01`

| Сигнатура | `rpc MethodName(Request) returns (Response)` |
| :--- | :--- |
| **Сообщение запроса** | `RequestMessage` (key fields) |
| **Сообщение ответа** | `ResponseMessage` (key fields) |

## Логика обработки запроса

1. … (numbered steps; Redis/DB/external calls)

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | … |
| **UNAUTHENTICATED** | … |
| **PERMISSION_DENIED** | … |
| **INTERNAL** | … |
```

Anchor `#grpc-[MethodName]` is **mandatory** — REST/Socket rules link here.

# Proto (`Методы API/gRPC/{service}_service.proto`)

- Location: same folder as gRPC markdown (multiple `.proto` + `import` allowed, e.g. Agora).
- Header: `syntax = "proto3";`, `package`, `option csharp_namespace`.
- Enums: first value `_UNSPECIFIED = 0`.
- Every `rpc` in proto = row in gRPC markdown + anchor `#grpc-*`.
- Optional md wrapper: `02 - Спецификация proto (….proto).md` with fenced proto block.
- proto ↔ markdown mismatch → ISSUE in `99 - Staging`.
