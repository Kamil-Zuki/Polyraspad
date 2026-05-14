# MVP Step 3 — SRS (review-from-context + FSRS)

Plan ID: `reader-mvp-srs-review-2026-05-14`
Status: backlog
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`reader-lingq-hub-2026-05-14.md`](./reader-lingq-hub-2026-05-14.md)

## Goal

**Не разрывать цикл** чтение → повторение: стабильный вход в review из **reader/Library**, корректный **контекст/source** карточки, **возврат в чтение**, счётчик **due**; расписание **FSRS** только через микросервис **`inclusive`** (gRPC).

## Out of Scope

- Импорт, подсветка, Mine — планы `reader-mvp-read-2026-05-14`, `reader-mvp-mining-2026-05-14`.
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

- Backlog: `.cursor/tasks/backlog/reader-mvp-srs-review-2026-05-14/`

## Verification

- Ручной сценарий: из reader открыть review → оценить → вернуться; due обновляется.
- Интеграционные проверки Aggregator/Vocabulary при изменении контракта оценок.

## References

- `inclusive/README.md`
- `VocabularyService/Options/InclusiveOptions.cs`
- `Docs/reader-library-lingq-roadmap.md`

## Cleanup

- [ ] Перенос в `active/` при старте; по завершении — `archive/`.
