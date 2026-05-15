# 03 — MVP Step 3 — SRS (review-from-context + FSRS)

Plan ID: `03-reader-mvp-srs-review-2026-05-14`
Priority: **03**
Status: active
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`00-reader-lingq-hub-2026-05-14.md`](../backlog/00-reader-lingq-hub-2026-05-14.md)

## Goal

**Не разрывать цикл** чтение → повторение: стабильный вход в review из **reader/Library**, корректный **контекст/source** карточки, **возврат в чтение**, счётчик **due**; расписание **FSRS** только через микросервис **`inclusive`** (gRPC).

## Session Review (изучение слов / карточек)

**Уже реализовано:** сессия повторения с карточкой, прогрессом по сессии и кнопками **AGAIN / HARD / GOOD / EASY** с интервалами (тот же UX, что ожидается от FSRS). В продуктовых текстах можно называть **Session Review** — режим изучения по карточкам в очереди.

**До запуска:** убедиться, что очередь и интервалы согласованы с **FSRS** через **`inclusive`**; сценарий **reader → Session Review → reader** и **due** без разрывов. Отдельно (с планом **04**): **мобильный web** для Session Review — основной сценарий «учить в метро» (создание карточек может оставаться комфортным на десктопе).

## Out of Scope

- Импорт, подсветка, Mine — планы **`01-reader-mvp-read-2026-05-14`**, **`02-reader-mvp-mining-2026-05-14`** → [`01-reader-mvp-read-2026-05-14.md`](../archive/01-reader-mvp-read-2026-05-14.md), [`02-reader-mvp-mining-2026-05-14.md`](../archive/02-reader-mvp-mining-2026-05-14.md).
- Замена ядра FSRS или самописный планировщик — запрещено; UI «карточка + AGAIN/HARD/GOOD/EASY» **уже есть** — не переписывать с нуля.

## Граница шага

Сессия повторения и расписание **после** того, как карточки/термины в колоде/проекте.

## MVP обязательно

- **Session Review / UI review-сессии** уже в продукте (прогресс сессии, **AGAIN / HARD / GOOD / EASY** с интервалами). Задача плана — **интеграция**: запуск из reader, due, возврат, source URL.
- **FSRS:** расчёт через [`inclusive/`](../../../inclusive/README.md); настройки проекта из домена; не дублировать планировщик в приложении.
- **Study на телефоне:** верстка и жесты Session Review, Dashboard и browser reader — приемлемы на малых экранах (детали и PWA — план **04**).

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

- Active: `.cursor/tasks/active/03-reader-mvp-srs-review-2026-05-14/` (при необходимости)

## Verification

- Ручной сценарий: из reader открыть review → оценить → вернуться; due обновляется.
- **Session Review** на типовом телефоне (узкая ширина): карточка и кнопки оценок без горизонтального скролла; совместно с планом **04** (PWA при появлении).
- Интеграционные проверки Aggregator/Vocabulary при изменении контракта оценок.

## References

- `inclusive/README.md`
- `VocabularyService/Options/InclusiveOptions.cs`
- `Docs/reader-library-lingq-roadmap.md`

## Cleanup

- [ ] По завершении — `archive/`.
