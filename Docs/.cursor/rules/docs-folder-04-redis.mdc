---
description: "[G3 · 04 · Redis] Operation block template, keys/TTL/fail mode"
globs: "**/04 - Бекенд, API и Контракты/**/Работа с Redis/**"
alwaysApply: false
---

# Redis (`Работа с Redis/`)

In-memory layer: sessions, rate limits, caches, tickets, idempotency. Key naming and TTL policies must match `02` КАР.

## `00 - Работа с Redis - Общая информация.md`

1. `# Введение` — role, naming conventions, TTL policy.
2. `# 1. Группы операций` — summary table.
3. `# 2 … N` — per-group: SR/КАР | Operation summary.

## Group file `NN - [Group name].md`

1. `# Введение`
2. `# 1. Список операций` — operation table.
3. Each operation — block below; separate with `---`.

## Operation Block Template

```markdown
# [Название операции] ([SR-CODE] / КАР-N)

## Общая информация

| Тип операции | Read \| Write \| Delete |
| :--- | :--- |
| **Ключ Redis** | `prefix:{id}` (document pattern) |
| **DTO / значение** | JSON `StateDto` \| string marker |
| **TTL** | static \| sliding \| until token `exp` |

## Логика обработки запроса

* Commands: GET / SETEX / DEL / EXISTS
* Cache-aside on miss (fallback to PostgreSQL if documented)
* Invalidation triggers (logout, backchannel, revoke)

## Статус-коды при ошибках

| Код | Описание | Fail-open \| Fail-closed |
| :--- | :--- | :--- |
| **UNAVAILABLE** | Redis cluster down | … |
```

Document fail-open vs fail-closed explicitly for security-sensitive keys (sessions, blacklist).
