# Reader / Library — coordination hub (split plans)

Plan ID: `reader-lingq-hub-2026-05-14`
Status: backlog
Created: 2026-05-14
Owner: `lead-agent`

## Goal

Синхронизировать реализацию с [Docs/reader-library-lingq-roadmap.md](../../../Docs/reader-library-lingq-roadmap.md): закрыть разрывы до LingQ-style UX. Монолитный план **`reader-library-lingq-roadmap-2026-05-13`** (ранее в `active/`) **разбит** на дочерние планы в **backlog**; при старте работы переносите выбранный план в `active/` по правилам [`.cursor/plans/README.md`](../README.md).

## Child plans (backlog)

| MVP / тема | Plan ID | Файл |
|------------|---------|------|
| Шаг 1 — Чтение (форматы, reader, подсветка) | `reader-mvp-read-2026-05-14` | [`reader-mvp-read-2026-05-14.md`](./reader-mvp-read-2026-05-14.md) |
| Шаг 2 — Mining (pop-up, Mine, term actions) | `reader-mvp-mining-2026-05-14` | [`reader-mvp-mining-2026-05-14.md`](./reader-mvp-mining-2026-05-14.md) |
| Шаг 3 — SRS (review-from-context, FSRS, inclusive) | `reader-mvp-srs-review-2026-05-14` | [`reader-mvp-srs-review-2026-05-14.md`](./reader-mvp-srs-review-2026-05-14.md) |
| Phase 3–4 — library IA, polish | `reader-library-phases34-2026-05-14` | [`reader-library-phases34-2026-05-14.md`](./reader-library-phases34-2026-05-14.md) |

## Core Learning Loop (сводка)

```mermaid
flowchart LR
  S1["1 Чтение"]
  S2["2 Mining"]
  S3["3 SRS"]
  S1 --> S2 --> S3
```

| # | Шаг | План |
|---|-----|------|
| **1** | Чтение | `reader-mvp-read-2026-05-14` |
| **2** | Mining | `reader-mvp-mining-2026-05-14` |
| **3** | SRS | `reader-mvp-srs-review-2026-05-14` |

## Progress (перенесено с 2026-05-13 — 2026-05-14)

- **Phase 0 (REST bridge):** `TextController` (`POST /api/text/analyze`), `TermsController`, `MediaServiceClientImpl`, маппинги; `MediaControllerTests` зелёные. Детали контрактов — в дочерних планах (read / mining / srs).
- **Frontend:** восстановлены `text-client.ts`, `term-client.ts`.
- **SRS:** FSRS через **`inclusive/`**; UI review-сессии (оценки, интервалы) уже в продукте; дожать **review-from-context** — план `reader-mvp-srs-review-2026-05-14`.

## Verification (сквозное)

- Три MVP-гейта — по критериям в планах шагов 1–3.
- Phase 0 гейт: endpoints из `constants.ts` не 404 — при работах по Aggregator bridge.
- Детальные чеклисты — внутри каждого дочернего плана.

## References

- `Docs/reader-library-lingq-roadmap.md`
- `Docs/ux/lingq-style-acceptance-criteria.md`
- `Docs/testing/reader-library-tdd-matrix.md`
- `context/plans/active/lingq-reader-implementation-plan.md` (если есть в ветке)

## Cleanup

- [ ] При взятии плана в работу: `backlog/<id>.md` → `active/<id>.md`, `Status: active` (см. README).
- [ ] При закрытии: `active/` → `archive/`, решения при необходимости в `context/decisions/` или `Docs/`.
