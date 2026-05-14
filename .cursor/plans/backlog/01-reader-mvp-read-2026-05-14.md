# 01 — MVP Step 1 — Чтение (Read)

Plan ID: `01-reader-mvp-read-2026-05-14`
Priority: **01**
Status: backlog
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`00-reader-lingq-hub-2026-05-14.md`](./00-reader-lingq-hub-2026-05-14.md)

## Goal

Пользователь стабильно открывает урок и читает **извлекаемый** текст; в reader — подсветка **NEW / SAVED / KNOWN** (и при необходимости IGNORED), phrase-приоритет по term-first.

## Out of Scope

- Pop-up словарь и Mine — план **`02-reader-mvp-mining-2026-05-14`** → [`02-reader-mvp-mining-2026-05-14.md`](./02-reader-mvp-mining-2026-05-14.md).
- Запуск review и оценки карточек — план **`03-reader-mvp-srs-review-2026-05-14`** → [`03-reader-mvp-srs-review-2026-05-14.md`](./03-reader-mvp-srs-review-2026-05-14.md).
- Phase 5 (Sentence View, YouTube, offline и т.д.) — только общий backlog.

## Граница шага

Всё, что нужно, чтобы пользователь **видел и листал текст**, понимал прогресс и статусы слов — **до** сохранения учебных единиц и **до** повторений.

## MVP обязательно

- Источники текста: **`.epub`**, **`.txt`**, **Paste text**; PDF — с явными ограничениями («no extractable text»); **OCR** — отдельный backlog после MVP.
- Reader: подсветка статусов; в UI SAVED = жёлтый «изучаемый» термин со значением; phrase-приоритет — `.cursor/rules/06-lingq-domain-guardrails.mdc`.
- **До запуска (резюме):** импорт **`.epub`** и **чистого текста** — в приоритете качества; **PDF** без OCR — второй план, чтобы не ломать первый опыт.

## Onboarding: не «чистый лист»

- На пустом Dashboard новый пользователь демотивируется.
- **MVP:** положить в библиотеку по умолчанию **2–3 коротких текста** (новости, простой диалог или мини-сказка) — контент уже открывается в reader по плану чтения; сценарий приветственной колоды см. план **04** (координация с продуктом).

## Связь с фазами

| Phase | Что относится к этому плану |
|-------|------------------------------|
| 0 | REST bridge, `/api/text/analyze`, media/reader endpoints — база для открытия урока |
| 1 | bulk known on page turn (настройка), phrase highlight, resume PDF |
| 3 | Импорт/форматы и library pipeline для EPUB/TXT/paste |

## Agents

- `product-agent`: приёмка шага 1 (извлекаемый текст, подсветка, PDF messaging).
- `backend-agent`: MediaService, text pipeline, при необходимости импорт EPUB/TXT.
- `frontend-agent`: reader rendering, pagination, статусы токенов.
- `reviewer-agent`: регрессии term-first, reader-library TDD matrix.

## Contracts To Lock

- `Docs/api/reader-aggregator-contract.md`; пути в `polyraspad-frontend` (`constants.ts`).
- **MVP контент:** `.epub`, `.txt`, Paste text; ограничения PDF; OCR — backlog.
- DTO терминов: WORD | PHRASE, дубликаты по `NormalizedText`.
- Настройка «Mark blue as known on page turn» + семантика bulk.

## Tasks

- Backlog (при появлении): `.cursor/tasks/backlog/01-reader-mvp-read-2026-05-14/`

## Verification

- Импорт/вставка → открытие reader → корректный текст (не «пустой» PDF без извлечения, если не поддерживаем).
- Подсветка NEW/SAVED/KNOWN согласована с анализом; phrase приоритетнее слов.
- Для onboarding: дефолтные тексты из библиотеки открываются тем же reader-потоком, что и пользовательский импорт.
- `npm test` по зонам reader при изменениях.

## References

- `Docs/reader-library-lingq-roadmap.md`
- `Docs/reader/reader-product-spec-v2.md`
- `Docs/ux/lingq-style-acceptance-criteria.md`

## Cleanup

- [ ] Перенос в `active/` при старте; по завершении — `archive/` (см. hub).
