# 04 — Reader / Library — Phase 3–4 (content-first + polish)

Plan ID: `04-reader-library-phases34-2026-05-14`
Priority: **04** (после стабилизации MVP **01–03**)
Status: backlog
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`00-reader-lingq-hub-2026-05-14.md`](./00-reader-lingq-hub-2026-05-14.md)

## Goal

После стабилизации MVP-шагов 1–3: **Phase 3** — content-first library (Continue reading, прогресс, IA); **Phase 4** — polish и производительность (кэш анализа, virtual scroll, optimistic UI, a11y).

## Onboarding и «пустой» Dashboard

- Новый пользователь не должен видеть только нули без подсказки «что делать дальше».
- **MVP:** **приветственная колода** из **~5 карточек**, демонстрирующая интерфейс изучения (**Session Review**) и базовые оценки.
- **2–3 дефолтных коротких текста** в библиотеке — **зафиксировано с планом 01** ([`../archive/01-reader-mvp-read-2026-05-14.md`](../archive/01-reader-mvp-read-2026-05-14.md); AC и `seedRole` — `Docs/ux/lingq-style-acceptance-criteria.md`): открытие **тем же reader-потоком**, что пользовательский импорт; реализация seed не дублируется между планами — один владеющий PR-поток (**04** после MVP 01), второй только ссылается.

## UX Library (упрощение до MVP)

- Сейчас пересечение понятий: Library, Collections, Project shelf, Books — звучит как дубли сущностей.
- **MVP:** свести к понятной паре уровней — например **Texts/Books** и **Folders/Tags** (или один эквивалент «контент» + «организация»); детали IA — `Docs/library/library-content-first-ia.md`.

## Mobile Web и PWA

- Большинство **повторений** (Session Review) делается с телефона; **mining** может оставаться десктоп-френдли.
- **MVP:** адаптив **Dashboard**, **Library**, **reader** и **экран Study / Session Review**; **PWA** как целевой компромисс вместо нативных приложений на запуске.

## Out of Scope

- Phase 5 (Advanced): Multi-context, YouTube, **нативные** клиенты (отдельно от PWA), полноценный offline — backlog, не обязательства этого плана.
- Леммы как основа статуса — запрещено (см. `.cursor/rules/06-lingq-domain-guardrails.mdc`).

## Phases (сводка)

| Phase | Фокус | Критерий «готово» (кратко) |
|-------|--------|---------------------------|
| 3 | Content-first library | Continue reading, прогресс на карточках/уроках, IA |
| 4 | Polish / performance | Кэш анализа, virtual scroll, optimistic UI, a11y; **PWA**; мобильная полировка Session Review |

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
- Onboarding: новый аккаунт видит дефолтные тексты и может открыть приветственную колоду → **Session Review**.
- Нагрузочный/ручной смоук virtual scroll и кэша — по мере реализации.
- **PWA / install prompt** (когда включено): smoke установки и открытия Session Review с домашнего экрана.

## Risks

- `MediaServiceClientImpl`: инкрементальная реализация / stub.
- Производительность анализа текста: не блокировать MVP планов **01–03**.

## References

- `Docs/library/library-content-first-ia.md`
- `Docs/reader-library-lingq-roadmap.md`

## Cleanup

- [ ] Перенос в `active/` при старте; по завершении — `archive/`.
