# Product Agent Task

Plan ID: `reader-library-lingq-roadmap-2026-05-13`
Agent: `product-agent`
Status: done
Can run in parallel: yes

## Objective

Зафиксировать приёмочные критерии по фазам roadmap и трассировку к `Docs/ux/lingq-style-acceptance-criteria.md`, без расширения scope за пределы `Docs/reader-library-lingq-roadmap.md`.

## Inputs

- Plan: `.cursor/plans/active/reader-library-lingq-roadmap-2026-05-13.md`
- Files/contracts to read:
  - `Docs/reader-library-lingq-roadmap.md`
  - `Docs/ux/lingq-style-acceptance-criteria.md`
  - `Docs/reader/reader-product-spec-v2.md`
  - `Docs/library/library-content-first-ia.md`

## Scope

- Чеклисты Phase 0–4: пользовательские формулировки DoD, явные **Out** для Phase 5.
- WORD vs PHRASE: правила создания, отображения (приоритет фразы), отсутствие lemma-first поведения в копиях/подсказках.
- Настройка «Mark remaining blue as known on page turn»: ожидаемое поведение при выкл/вкл.
- Phase 2: user story «review из reader» + возврат + счётчик due (lag/lead метрики из roadmap — как ориентиры, не как код).

## Out of Scope

- Детальное API-дизайн (отдаётся контракту + backend); изменение кода.

## Deliverables

- Краткий документ или комментарии в task handoff: таблица **Phase → AC id/раздел** + открытые продуктовые вопросы только если блокируют Phase 0/1.

## Verification

- Согласованность с `.cursor/rules/06-lingq-domain-guardrails.mdc` (term-first).

## Handoff

- Список AC с приоритетом для Phase 0 и Phase 1; явные blockers для `lead-agent`.
