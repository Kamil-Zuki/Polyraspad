# 01 — MVP Step 1 — Чтение (Read)

Plan ID: `01-reader-mvp-read-2026-05-14`
Priority: **01**
Status: archived
Created: 2026-05-14
Archived: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`00-reader-lingq-hub-2026-05-14.md`](../backlog/00-reader-lingq-hub-2026-05-14.md)

## Goal

Пользователь **надёжно** импортирует и открывает урок из **`.epub`**, **`.txt`** и **Paste text**; в reader — стабильная подсветка **NEW / SAVED / KNOWN** (и при необходимости IGNORED), **phrase-приоритет** по term-first; настройка и **bulk** «синие → KNOWN при смене страницы». **PDF** — только с **честными ограничениями** (нет извлекаемого текста → явное сообщение в UI, без «тихого» пустого reader); **OCR** — вне этого спринта.

## Cross-plan: 04 (onboarding — 2–3 дефолтных текста)

- **Договорённость:** в библиотеке по умолчанию **2–3 коротких текста** (типы: новость / простой диалог / мини-сказка — финальный выбор в **04** с product).
- **Контракт с 01:** каждый такой текст открывается **тем же reader-потоком**, что пользовательский импорт (те же endpoints, тот же анализ/подсветка); идентификаторы/пути согласуются в `Docs/api/reader-aggregator-contract.md` или follow-up в **04** при появлении library seed API.
- **Владение реализацией seed:** минимальный **seed контента** может лечь в **01** (если только reader+media), либо в **04** (если требуется library IA); lead-agent: не дублировать — один PR-поток владеет файлами, второй план только ссылается.

## Out of Scope

- Pop-up словарь и Mine — план **`02-reader-mvp-mining-2026-05-14`**.
- Запуск review и оценки карточек — план **`03-reader-mvp-srs-review-2026-05-14`**.
- **OCR** для PDF — отдельный backlog после MVP.
- Phase 5 (Sentence View, YouTube, offline и т.д.) — общий backlog.

## Граница шага

Всё, что нужно, чтобы пользователь **видел и листал текст**, понимал прогресс и статусы слов — **до** mining и **до** SRS-повторений.

## Agents

- `product-agent`: приёмка (форматы, PDF messaging, подсветка, bulk known).
- `backend-agent`: MediaService / text pipeline, импорт EPUB/TXT, ответы при отсутствии извлекаемого текста в PDF.
- `frontend-agent`: reader rendering, pagination, токены/статусы, настройка page-turn.
- `reviewer-agent`: регрессии term-first, reader TDD matrix.

## Contracts To Lock

- `Docs/api/reader-aggregator-contract.md`; пути в `polyraspad-frontend` (`constants.ts`).
- DTO терминов: WORD | PHRASE; дубликаты по `NormalizedText` (guardrails).
- Настройка «Mark blue (NEW) as known on page turn» + семантика **BulkMarkKnown** (идемпотентность, границы «страницы»).
- PDF: контракт ошибки / флага «no extractable text» (не 200 с пустым телом без объяснения).

## Tasks

- Archived: `.cursor/tasks/archive/01-reader-mvp-read-2026-05-14/`

## Execution status (final)

| Task file | Agent | Status |
|-----------|--------|--------|
| `product.md` | product-agent | **done** |
| `backend.md` | backend-agent | **done** |
| `frontend.md` | frontend-agent | **done** |
| `review.md` | reviewer-agent | **done** (lightweight checklist at close) |

## Verification (executed at archive)

- `dotnet test AggregatorService.Tests --filter FullyQualifiedName~MediaControllerTests`
- `npx vitest run` under `polyraspad-frontend` for `src/app/reader/*.test.ts(x)` (see task files for exact paths)

## References

- `Docs/reader-library-lingq-roadmap.md`
- `Docs/reader/reader-product-spec-v2.md`
- `Docs/ux/lingq-style-acceptance-criteria.md`
- `.cursor/rules/06-lingq-domain-guardrails.mdc`

## Completion summary

- **Backend (Aggregator):** `POST /api/Media/extract-document-text` (PDF/EPUB/TXT), **422** `NoExtractableText` when empty; **Vocabulary** `BulkMarkKnown` single transaction; tests + contract doc updated.
- **Frontend:** `/reader` — EPUB/TXT (client extract) + PDF flows, phrase-first rendering when `phrases` present, page-turn bulk + React Query invalidation, explicit PDF empty/error panels; Vitest coverage on reader utils/page.
- **Product:** MVP Read acceptance criteria and seed roles documented in `Docs/ux/lingq-style-acceptance-criteria.md` (§ MVP Read + AC-1 terminology + AC-6 page slice).

## Follow-ups (outside this archived plan)

- Persist **mark known on page turn** in `UserSettings` gRPC/DTO (today: `localStorage` + documented in AC).
- **`phrases`** in analyze pipeline when vocabulary exposes them; **PHRASE** bulk if product requires yellow phrases on page turn.
- **Library upload** EPUB/TXT on Media vs client-only — product decision.
- **Seed content files** + library cards — **plan 04**.

## Cleanup

- [x] Перенос в `active/` при старте
- [x] Task-папка → `archive/`
- [x] План → `plans/archive/`
