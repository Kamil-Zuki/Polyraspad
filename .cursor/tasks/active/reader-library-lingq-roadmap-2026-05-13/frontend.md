# Frontend Agent Task

Plan ID: `reader-library-lingq-roadmap-2026-05-13`
Agent: `frontend-agent`
Status: done
Can run in parallel: no

## Objective

После готовности Phase 0 backend: убедиться, что reader/library используют Aggregator endpoints без 404; затем реализовать **Phase 1** (bulk known на перелистывании + настройка, phrase selection и `PHRASE` в мутациях, приоритет подсветки фраз, resume PDF) и подготовить основу под Phase 2–3 по roadmap.

## Inputs

- Plan: `.cursor/plans/active/reader-library-lingq-roadmap-2026-05-13.md`
- Files/contracts to read:
  - `Docs/api/reader-aggregator-contract.md`
  - `polyraspad-frontend` — `constants.ts`, API-клиенты reader/terms, `src/app/reader/`
  - `Docs/library/library-content-first-ia.md` (Phase 3)

## Scope

- Сверка всех вызовов с `constants.ts` и контрактом; удаление/замена заглушек, ожидающих 404.
- Подключить `bulkMarkKnown` на page turn с user setting (согласовать поле с backend после контракта).
- Phrase workflow: снять хардкод `type="WORD"` где создаётся LingQ; Shift+click (или утверждённый паттерн) для выделения фразы; приоритет отображения фразы над отдельными словами.
- Resume PDF: `lastPageNumber` (или поле из контракта) — сохранение и восстановление.
- Phase 3 prep: только после Phase 1–2 стабильности — не ломать текущий deck-first до готовности IA задач.

## Out of Scope

- Реализация backend контроллеров (owner: `backend-agent`).
- Phase 5 фичи.

## Deliverables

- Изменения в Next.js с `'use client'` только где нужно; React Query инвалидации для терминов/анализа.
- Компонентные/интеграционные тесты по риску (reader interactions, term status).

## Verification

- `npm test` с узким паттерном (`reader`, `term` — по правилам репо).
- Ручная проверка: анализ текста, создание термина, библиотека книг — HTTP 200 через UI.

## Handoff

- Список экранов/хуков, затронутых Phase 1; известные UI edge cases для `reviewer-agent`.
