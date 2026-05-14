# MVP Step 2 — Mining

Plan ID: `reader-mvp-mining-2026-05-14`
Status: backlog
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`reader-lingq-hub-2026-05-14.md`](./reader-lingq-hub-2026-05-14.md)

## Goal

Из текста в минимум жестов: **pop-up словарь** по клику и **«Создать карточку (Mine)»** в один клик; согласованность с term actions без поломки Phase 1 контрактов.

## Out of Scope

- Импорт форматов и «голый» reader без mining UI — план `reader-mvp-read-2026-05-14`.
- Расчёт FSRS и прохождение review-сессии — план `reader-mvp-srs-review-2026-05-14`.

## Граница шага

Всё, что пользователь делает, чтобы **извлечь из контекста** слово/фразу в учёт (термин, значение, опционально карточка) — **без** интервального повторения.

## MVP обязательно

- По клику на слово: **pop-up словарь** (lookup / перевод).
- Из pop-up: **Mine** в **один клик** (term-first: термин + опциональная карточка).
- Согласованность с **CreateOrUpdateTerm → SAVED**, **MarkTermKnown**, **IgnoreTerm**, **phrase**, если инспектор остаётся параллельно.

## Связь с фазами

| Phase | Фокус |
|-------|--------|
| 1 | Core reader UX: pop-up, Mine, phrase, приоритет фразы над словами |

## Agents

- `product-agent`: приёмка шага 2 (один клик Mine, нет «мёртвых» веток).
- `backend-agent`: terms API, создание карточки из контекста.
- `frontend-agent`: pop-up слой, инспектор, связка с `term-client`.
- `reviewer-agent`: контракты DTO/API client.

## Contracts To Lock

- Terms CRUD/mutations, `bulkMarkKnown` (см. Aggregator contract).
- UI: инспектор без lemma labels; **pop-up** + **Mine** из одного слоя.
- `type` WORD | PHRASE, дубликаты по `NormalizedText`.

## Tasks

- Backlog: `.cursor/tasks/backlog/reader-mvp-mining-2026-05-14/`

## Verification

- E2E при наличии: import → read → сохранённый термин (SAVED) / Mine.
- `npm test` — reader/terms.

## References

- `Docs/ux/lingq-style-acceptance-criteria.md`
- `Docs/api/reader-aggregator-contract.md`

## Cleanup

- [ ] Перенос в `active/` при старте; по завершении — `archive/`.
