---
description: "[G3 · 04 · DTO] Block template, якоря #dto-*"
globs: "**/04 - Бекенд, API и Контракты/**/Методы API/DTO/**"
alwaysApply: false
---

# DTO (`Методы API/DTO/`)

Гroups and file order match capability groups in `01`. Fields align with `03` entities and gRPC/proto message types.

## `00 - DTO - Общая информация.md`

1. `# Введение` — role of DTO, Gateway REST/WebSocket ↔ gRPC; links to SR and КАР.
2. `# 1. Группы DTO` — summary table (group name = `NN - …` files).
3. `# 2 … N` — per-group DTO tables (name | purpose | Request/Response).

## Group file `NN - [Group name].md`

1. `# Введение` — scope and SR links from `01`.
2. `# 1. Список DTO` — table of all DTOs before detail blocks.
3. Each DTO — block below; separate blocks with `---`.

## DTO Block Template

```markdown
<span id="dto-[DtoName]"></span>

# DTO: [DtoName]

## Контекст и назначение

(When used; consumer — SPA, Gateway, internal call)

**Назначение:** Запрос | Ответ | Событие | …
**Реализация сущности:** Table `entity_name` / N/A / proto field `Message.field`

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `fieldName` | `uuid` \| `string` \| … | … |

## Пример работы (JSON)

\`\`\`json
{
  "fieldName": "…"
}
\`\`\`
```

- Wikilink to REST route / `#grpc-*` / `03` entity.
- Nested DTO: type `[DtoName]` or `array<DtoName>`.
- `ProblemDetailsDto` / `SuccessResponseDto` — only in «Основные (общие) DTO».
