# Product Task

Plan ID: `01-reader-mvp-read-2026-05-14`
Agent: `product-agent`
Status: done
Can run in parallel: yes

## Completion

AC и seed-роли зафиксированы в `Docs/ux/lingq-style-acceptance-criteria.md` (§ **MVP Read — Plan 01**; уточнения AC-1 терминологии LEARNING/SAVED; AC-6 — bulk только с уходящей страницы; персистентность настройки до `/api/settings`).

## Objective

(архив) Зафиксировать приёмочные критерии шага «Чтение»: форматы, PDF, подсветка, bulk known; согласовать с **04** список **2–3 дефолтных текстов** (роли контента, не обязательно финальные файлы).

## Inputs

- Plan: `.cursor/plans/archive/01-reader-mvp-read-2026-05-14.md`
- `Docs/ux/lingq-style-acceptance-criteria.md`
- План **04**: `.cursor/plans/backlog/04-reader-library-phases34-2026-05-14.md` (onboarding)

## Handoff

- `seedRole`: `news` | `dialogue` | `mini_story`; метаданные: `language`, `level`, `title`, `projectId` — см. таблицу в UX-доке.
