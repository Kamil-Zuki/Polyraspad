---
description: "[G3 · 04 · Algorithms] I/O tables, pseudocode, links to gRPC/КАР"
globs: "**/04 - Бекенд, API и Контракты/**/Алгоритмы и методы бекенда/**"
alwaysApply: false
---

# Алгоритмы (`Алгоритмы и методы бекенда/`)

Server-side implementation logic behind gRPC, Redis, and Rabbit. Reference SR from `01` and КАР from `02`.

## `00 - Алгоритмы и методы бекенда - Общая информация.md`

1. `# Введение` — audience (backend, AppSec, ops).
2. `# 1. Группы алгоритмов` — summary table.
3. `# 2 … N` — per-group algorithm name | short description.

## Group file `NN - [Group name].md`

1. `# Введение`
2. `# 1. Список алгоритмов` — algorithm table.
3. Each algorithm — block below; separate with `---`.

## Algorithm Block Template

```markdown
# Алгоритм [название]

## Контекст и область применения

### Почему был создан

### Бизнес-требование

(SR-CODE from `01`)

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | … |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | … |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| … | … | … | Да \| Нет |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| … | … | … |

## Логика работы (Псевдокод)

\`\`\`csharp
// numbered steps or middleware pseudocode
\`\`\`

## Связанные артефакты

* gRPC: `#grpc-*`
* Redis keys: `prefix:{id}`
* Rabbit: queue/topic name
* КАР-N from `02`
```

Algorithms explain **how**; gRPC docs define **contract**. Avoid duplicating full RPC specs — link instead.
