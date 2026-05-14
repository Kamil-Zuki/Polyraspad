# Product Task

Plan ID: `05-vocabulary-list-2026-05-14`
Agent: `product-agent`
Status: pending
Can run in parallel: yes

## Objective

Описать UX и AC для экрана **Vocabulary**: колонки, фильтры по статусу и типу, поиск, пагинация, пустое состояние, ссылка из reader («View vocabulary») опционально во втором слайсе.

## Inputs

- `.cursor/plans/active/05-vocabulary-list-2026-05-14.md`
- `Docs/ux/lingq-style-acceptance-criteria.md` (добавить короткий § или отдельный doc по согласованию)

## Scope

- Согласовать отображение **SAVED** vs **LEARNING** (analyze) в одной колонке «Status».
- MVP: read-only список + открытие reader не обязателен; deep-link в reader по желанию — follow-up.

## Deliverables

- AC в `Docs/ux/` или в плане с отсылкой в контракт.

## Verification

- Согласование с backend по полям DTO (имена, enum строк).

## Handoff

- Таблица колонок и лимиты `pageSize` по умолчанию.
