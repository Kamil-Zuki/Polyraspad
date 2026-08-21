---
description: "[G0 · Core] Порядок 03→01→02→04, BFF, anti-hallucination, staging"
alwaysApply: true
---

# Role & Tone

- Senior System Analyst and Software Architect.
- Output ONLY pure Markdown when generating docs. No greetings, no meta-commentary, no conclusions.

# MANDATORY — `01` ↔ `03` and Staging

**`03` is read-only unless the user explicitly asks to edit `03`.** Typical task: edit **`01`** using **`03`** as reference — on mismatch → **ISSUE in `99`**, never silent edits to `03`.

When you **create or edit** `01` (or `03` only when explicitly requested):

1. **Read** `03` for the same service (when editing `01`).
2. On **any** `01`↔`03` mismatch → **write** `ISSUE-NNN-*.md` + update `00 - Реестр проблем.md` **in the same turn**.
3. Do **not** patch `03` to match new `01` text without an explicit user request to change `03`.

Detailed steps: `.cursor/rules/docs-staging-0103.mdc`.

# Documentation Order (Source of Truth)

Generate and validate in this dependency order:

1. **03 — Модель данных** — entities, fields, relations, lifecycles (primary source).
2. **01 — Функциональная спецификация** — SR from data model.
3. **02 — Архитектура** — flows, integrations, Gateway/gRPC, КАР.
4. **04 — Бекенд, API и Контракты** — DTO, REST, WebSocket, gRPC, RabbitMQ, Redis, `.proto`.

Do not write `04` before `01`/`03` are stable. Do not invent SR or entities not grounded in upstream folders.

# Architectural Constraints

- **Topology:** Client ↔ BFF (API Gateway) ↔ Microservices.
- **Communication:** BFF calls microservices **via gRPC** .
- **Thin BFF:** REST and WebSocket on Gateway have NO heavy business logic — parse, route, call gRPC.
- **Cross-reference:** In REST/WebSocket docs, always name the underlying gRPC method(s).

# Reference Artifacts (format only)

| Artifact                                  | Use                                                    |
| :---------------------------------------- | :----------------------------------------------------- |
| `(Done) Authorization Service/`           | **Layout and depth reference only** — same file path/type in target service |
| `Шаблон документации микросервиса/` | Short copies — markdown layout and table patterns only |
| `(Done)/*`                                | Do not rewrite without explicit user request           |

**Auth — не источник содержания для других сервисов:**

- Do **not** copy, paraphrase, or weave in Auth **text**: SR descriptions, scenarios, metaphors, DTO fields, endpoints, Redis keys, Rabbit flows, domain terms (sessions, OIDC, Phantom Token, external IdP, etc.).
- Do **not** paste Auth fragments as «examples» or «for reference» inside `{Target Service}/` documents.
- Use Auth only to see **how** a section is formatted (headings, tables, block order) when the target service has the **same document type** — then fill from target `03`/`01`/`02`.

**Never** copy business data, DTO fields, SR codes, or domain logic from Auth/template into a new service.

# Document Structure — Do Not Invent

Use **only** structures defined in:

1. Cursor rules (`.cursor/rules/` — block templates, folder tree in G2),
2. Existing files of the **target service**,
3. Copied шаблон / etalon **file tree** (names and hierarchy — not Auth body text).

Do **not**:

- Add headings, subsections, or document types **not** in the applicable rule template or target `00`/group file pattern.
- Import Auth-only sections because they «look good» (extra metaphors, bonus scenarios, unrelated КАР patterns).
- Create new folder names or `NN - …` files outside the service's `01` groups and etalon tree.

If the target service needs a structure Auth does not have — follow target `01`/`03`; if unclear → ISSUE in `99 - Staging`, do not improvise from Auth.

# Anti-Hallucination

- Use **exact** SR IDs, group names, and section order from `00 — Общая информация` / «Основные возможности» / «Возможности данного раздела».
- Never invent group names or IDs (e.g. `SR-AUTH-*` for a non-Auth service).
- On conflict with examples, prefer the target service's `01`/`03` and file an ISSUE in `99 - Staging`.
- Do **not** silently patch `01` or `03` to hide a cross-folder mismatch — record it in staging first.
- Do **not** edit **`03`** unless the user **explicitly** requests changes to `03` (entity, field, model). When writing `01`, treat `03` as read-only reference.

# Staging

**Принцип:** `01` и `03` **не должны конфликтовать**. Любой обнаруженный конфликт — **не замалчивать**: зафиксировать как ISSUE в `99`, а не «подправить» вторую папку без явного запроса пользователя.

Path: `<Сервис>/99 - Staging — Разрывы согласованности (DO NOT DELETE)/`. Agents must not delete this folder.

When **`03` and `01` disagree**, create `ISSUE-NNN-{slug}.md` and add a row to `00 - Реестр проблем.md`. **Do not edit `03`** to resolve the conflict unless the user explicitly asked to change `03`. Adjust `01` only when the user asked to fix `01` or close a specific ISSUE.

| Situation | Тип ISSUE |
| :--- | :--- |
| SR in `01` references entity/field absent in `03` | **Пробел** |
| Entity or field in `03` has no supporting SR in `01` (in scope) | **Пробел** |
| Same concept, different names or cardinality in `01` vs `03` | **Противоречие** |
| Group in `01/00` vs entity grouping in `03` misaligned | **Нейминг** |

Область `01`↔`03` задана rule `docs-staging-0103.mdc` — в ISSUE не дублировать. Стиль и навигация ISSUE (якорь **SR-ID** / **Entity.field**, язык для человека): `docs-staging-issues.mdc`. Другие области (`04`, `02`): `.cursor/skills/docs-04-verify/issue-template.md`.
