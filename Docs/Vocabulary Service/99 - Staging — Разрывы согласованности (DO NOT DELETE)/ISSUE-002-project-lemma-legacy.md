# ISSUE-002: ProjectLemma — legacy vs term-first продукт

## Тип

Противоречие

## В двух словах

Продукт (AGENTS.md / term-first) запрещает опираться на леммы для статусов и дубликатов, но сущность `ProjectLemma` остаётся в EF и может фигурировать в старых сценариях NLP.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-VOC-LING-01 | Term-first, леммы не основа статусов |
| 03 | Entity `ProjectLemma` | Таблица и связи с Card сохранены |

Путь к файлу (вторично): `01/…/SR-VOC-05_LinguisticsNLP.md`, `03/…/Entity - Лингвистическая Модель и NLP - Linguistics & NLP.md`

## Доказательство

`ProjectLemma` в snapshot; `TermNormalizer` / TermService работают от `ProjectTerm.NormalizedText`.

## Рекомендуемое действие

Зафиксировать deprecation/migration plan для `ProjectLemma` или ограничить её read-only legacy в `04`/коде.

## Статус

Open
