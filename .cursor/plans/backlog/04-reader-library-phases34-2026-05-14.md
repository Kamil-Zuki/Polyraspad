# 04 — Reader / Library — Phase 3–4 (content-first + polish)

Plan ID: `04-reader-library-phases34-2026-05-14`
Priority: **04** (после стабилизации MVP **01–03**)
Status: backlog
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`00-reader-lingq-hub-2026-05-14.md`](./00-reader-lingq-hub-2026-05-14.md)

## Goal

После стабилизации MVP-шагов 1–3: **Phase 3** — content-first library (Continue reading, прогресс, IA); **Phase 4** — polish и производительность (кэш анализа, virtual scroll, optimistic UI, a11y).

## Out of Scope

- Phase 5 (Advanced): Multi-context, YouTube, mobile reader, offline — только backlog, не обязательства этого плана.
- Леммы как основа статуса — запрещено (см. `.cursor/rules/06-lingq-domain-guardrails.mdc`).

## Phases (сводка)

| Phase | Фокус | Критерий «готово» (кратко) |
|-------|--------|---------------------------|
| 3 | Content-first library | Continue reading, прогресс на карточках/уроках, IA |
| 4 | Polish / performance | Кэш анализа, virtual scroll, optimistic UI, a11y |

## Agents

- `product-agent`: IA library, метрики прогресса, приёмка Phase 3–4.
- `backend-agent`: API library, кэш анализа при необходимости.
- `frontend-agent`: `/library`, навигация, производительность reader.
- `reviewer-agent`: регрессии навигации и кэша.

## Contracts To Lock

- `Docs/library/library-content-first-ia.md`
- Согласование с Aggregator contract при новых library endpoints.

## Tasks

- Backlog: `.cursor/tasks/backlog/04-reader-library-phases34-2026-05-14/`

## Verification

- UX review content-first vs deck-first (по критериям Docs).
- Нагрузочный/ручной смоук virtual scroll и кэша — по мере реализации.

## Risks

- `MediaServiceClientImpl`: инкрементальная реализация / stub.
- Производительность анализа текста: не блокировать MVP планов **01–03**.

## References

- `Docs/library/library-content-first-ia.md`
- `Docs/reader-library-lingq-roadmap.md`

## Cleanup

- [ ] Перенос в `active/` при старте; по завершении — `archive/`.
