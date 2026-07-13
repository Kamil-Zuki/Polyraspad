---
description: "[G1 · 01/02/03/05] SR-блоки, КАР, TOC, Obsidian"
globs: "**/{01 - Функциональная спецификация,02 - Архитектура,03 - Модель Данных,03 - Модель данных,05 - Сводная документация}/**/*.md"
alwaysApply: false
---

# MANDATORY — перед сохранением правок в `01` или `03`

См. **`.cursor/rules/steos-docs-staging-0103.mdc`** — сверка с парной папкой и **запись ISSUE на диск** обязательны при конфликте.

# Document Generation Workflow

For each document:

1. Write header, introduction, and **Table of Contents** first (TOC included).
2. Body sections follow **exact ID order** and **group names** from «Основные возможности» / «Возможности данного раздела» in the **target service's** `00` overview — not from Auth.
3. Keep all SR/requirements inside their parent groups — no cross-group mixing.
4. Follow **block templates in this rule and `.cursor/rules/`** — same heading levels and section order. Match Auth etalon **layout only** when an equivalent file exists; **never** transplant Auth paragraphs, scenarios, or domain examples into the target service.
5. **Do not invent** extra sections, tables, or sub-headings not in the template or the target service's established pattern.

# Folder 01 — Functional Specification

Each group file under `Возможности сервиса/`:

1. `# Группа N: …` — introduction; metaphor **only** if it fits **this service's** domain (do not reuse Auth metaphors verbatim).
2. `## Возможности данного раздела` — SR summary table (codes from `00`).
3. `# Детальная спецификация требований` — once per file, after the table.
4. Per SR — block structure below.

## SR Block Template

```markdown
## [SR-CODE]: [Название]

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| … | … |

### 2. Высокоуровневое описание

(Prose + metaphor where appropriate)

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: [Название] (Happy Path)
1. …

#### Сценарий Б: [Название] (Negative Path) — optional
1. …
```

**Сценарии — не обязательно несколько.** Достаточно **одного** логического сценария (часто только Happy Path), если SR простой или альтернативные/негативные ветки не добавляют смысла. Не выдумывай «Сценарий Б» ради шаблона — добавляй второй и далее только при реальной альтернативе, ошибке или edge case. Раздел `### 3. …` **не пропускай**, если хотя бы один сценарий нужен для понимания SR.

# Folder 02 — Architecture

Два типа файлов: **`00 - Общая архитектура и структура.md`** (обзор) и **`NN - КАР-N - …md`** (одно решение на файл). Нумерация и названия КАР — как в `01` capability groups; SR-коды из `01`, не выдумывать.

## Файл `00 - Общая архитектура и структура`

1. `# Обзор высокого уровня` — роль сервиса, ключевые паттерны, метрики/ограничения.
2. `# 1. Назначение документа` → `## 1.1 Цель документа` — для кого документ и что объясняет.
3. `# 2. Внутренняя структура и компоненты` — слои/компоненты списком с ответственностью каждого.
4. `# 3. Ключевые архитектурные решения (КАР)` — сводная таблица всех КАР со ссылками на отдельные файлы.

## КАР Block Template

Каждый файл `NN - КАР-N - [Название].md`:

```markdown
# Введение

(1–3 предложения: суть решения и зачем оно нужно)

## Контекст и проблема

(Какая архитектурная/безопасностная/эксплуатационная проблема без этого решения)

## Принятое решение

(Нумерованные шаги или пункты: что именно делаем, какие компоненты задействованы — Gateway, gRPC, Redis, RabbitMQ и т.д.)

## Обоснование и последствия

### Плюсы

* …

### Последствия

* …
* *Решение:* (компенсирующие меры — если применимо)
```

Дополнительно по необходимости (как в Auth):

- Mermaid / ASCII — потоки Gateway ↔ gRPC ↔ RabbitMQ/Redis.
- Wikilinks на SR в `01` и сущности в `03`.
- Якорь `{#КАР-N}` или `{#SR-CODE}` при перекрёстных ссылках из `04`/`05`.

# Folder 03 — Data Model

- One entity per document; align names and fields with `01` SR and `Entities` index.
- Document relations, constraints, lifecycle, integrations with other services.
- DDL (if provided) is authoritative for field names and types.

# Consistency `03` ↔ `01` → Staging

**`03` — источник истины, read-only без явного запроса пользователя.** Пишешь `01` → читаешь `03`; конфликт → **ISSUE в `99`**, не правка `03` молча.

**Check when editing `01`:**

1. Each SR that reads/writes persistent data names entities/fields that exist in `03`.
2. Each in-scope entity in `03` is covered by at least one SR in `01` (or marked out-of-scope in SR).
3. Group order in `01/00` matches entity grouping in `03`.

**On mismatch:**

1. **Write** ISSUE + update реестр (`steos-docs-staging-0103.mdc`, стиль — `steos-docs-staging-issues.mdc`).
2. Continue `01` if that was the task — **do not edit `03`** unless user explicitly requested `03` changes.

# Obsidian

- Use `[[wikilinks]]` for internal cross-references within the same service.
- Anchor headings with `{#SR-CODE}` when the Auth pattern uses explicit anchors.
