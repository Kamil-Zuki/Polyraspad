# 03 — MVP Step 3 — SRS (review-from-context + FSRS)

Plan ID: `03-reader-mvp-srs-review-2026-05-14`
Priority: **03**
Status: backlog
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`00-reader-lingq-hub-2026-05-14.md`](./00-reader-lingq-hub-2026-05-14.md)

## Goal

**Не разрывать цикл** чтение → повторение: стабильный вход в review из **reader/Library**, корректный **контекст/source** карточки, **возврат в чтение**, счётчик **due**; расписание **FSRS** только через микросервис **`inclusive`** (gRPC).

## Out of Scope

- Импорт, подсветка, Mine — планы **`01-reader-mvp-read-2026-05-14`**, **`02-reader-mvp-mining-2026-05-14`** → [`01-reader-mvp-read-2026-05-14.md`](./01-reader-mvp-read-2026-05-14.md), [`02-reader-mvp-mining-2026-05-14.md`](./02-reader-mvp-mining-2026-05-14.md).
- Замена ядра FSRS или самописный планировщик — запрещено; UI «карточка + AGAIN/HARD/GOOD/EASY» **уже есть** — не переписывать с нуля.

## Граница шага

Сессия повторения и расписание **после** того, как карточки/термины в колоде/проекте.

## MVP обязательно

- **UI review-сессии** уже в продукте (прогресс сессии, **AGAIN / HARD / GOOD / EASY** с интервалами). Задача плана — **интеграция**: запуск из reader, due, возврат, source URL.
- **FSRS:** расчёт через [`inclusive/`](../../../inclusive/README.md); настройки проекта из домена; не дублировать планировщик в приложении.

## Связь с фазами

| Phase | Фокус |
|-------|--------|
| 2 | Review из reader + контекст карточек; FSRS + inclusive |

## Agents

- `product-agent`: приёмка шага 3 (сквозной сценарий reader → review → reader).
- `backend-agent`: контракты review-from-context, вызовы inclusive.
- `frontend-agent`: навигация в/из review, счётчики due.
- `reviewer-agent`: регрессии SRS, контракты оценок.

## Contracts To Lock

- «Review из контекста», счётчик due, возврат в reader после сессии.
- Существующий UI review-сессии (оценки → FSRS).
- **FSRS** через **`inclusive`** (gRPC); см. `VocabularyService/Options/InclusiveOptions.cs`.

## Tasks

- Backlog: `.cursor/tasks/backlog/03-reader-mvp-srs-review-2026-05-14/`

## Verification

- Ручной сценарий: из reader открыть review → оценить → вернуться; due обновляется.
- Интеграционные проверки Aggregator/Vocabulary при изменении контракта оценок.

## References

- `inclusive/README.md`
- `VocabularyService/Options/InclusiveOptions.cs`
- `Docs/reader-library-lingq-roadmap.md`

## Cleanup

- [ ] Перенос в `active/` при старте; по завершении — `archive/`.
