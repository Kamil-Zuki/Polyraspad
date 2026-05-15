# 05 — Note-centric cards: follow-up backlog (P1–P5)

Plan ID: `05-note-centric-followup-backlog-2026-05-14`
Priority: **05** (после базовой свёртки CRUD/редактор/миграция колонок `cards` под `note.field_values`)
Status: archived
Created: 2026-05-14
Closed: 2026-05-15
Owner: `lead-agent`

Контекст: карточки в продуктовом потоке переведены на **Anki-like заметки** (`fieldValues` / `NotePayloadDto`); остаётся выровнять превью-контракты, захват в колоду, SRS-DTO, леммы и чистку protobuf.

## Goal

Закрыть разрыв между **полной карточкой (note-centric)** и остальными поверхностями (превью, capture, study, леммы, контракты), с приоритетом **видимой для пользователя корректности** и **единой истины данных**.

## Progress (2026-05-14 — closure 2026-05-15)

| Slice | Status |
|-------|--------|
| P1 — превью из `note.field_values` (в т.ч. marketplace preview + единый маппинг duplicate → gRPC) | **Done** (часть P1 по study/прочим поверхностям — см. P3) |
| P2 — capture: Inbox по умолчанию + опциональный `deckId` | **Done** |
| P3 — Study DTO | **Done** (контент и `source_meta` из note; `target_index` вычисляемый; `target_lemma`/REST `targetTerm` = форма из Word, fallback на лемму только если Word пуст; комментарии proto/DTO + тест) |
| P4 — Леммы / term-first cleanup | **Done** (новые/обновлённые карты: без `ResolveForCardAsync` и без write-on-read в `GetCardById`; Study sibling bury по `L:{lemma}` или `T:{projectTerm}`; `CardService` без `ILemmaService`) |
| P5 — Proto hygiene | **Deferred** (отдельный план при необходимости) |
| Редактор `/editor`: поля как в InOriginal (Source title/URL в основной форме) | **Done** |

## Closure verification (2026-05-15)

- `dotnet test VocabularyService.Tests --filter "FullyQualifiedName~CardService|FullyQualifiedName~StudyService"` — 16 passed.
- `npx tsc --noEmit` в `polyraspad-frontend` — OK.

## Out of Scope (пока)

- Полное удаление домена **леммы** из БД в одном PR (только поэтапный путь в **P4**).
- Перепись всей документации `Docs/` — при закрытии этапов по необходимости вынести решения в `context/decisions/` или `Docs/` отдельно.

## Contracts To Lock

- **P1:** `CardPreview` (gRPC) / `CardPreviewDto` (REST) — откуда брать строки превью (только производные из `notes.field_values` для Sentence Mining и согласованных типов заметок).
- **P2:** `CaptureCard` — целевая колода (**Inbox** vs опциональный `deckId`); синхрон Aggregator ↔ VocabularyService ↔ frontend.
- **P3:** `CardStudyDto` и производные **`sourceMeta` / `targetIndex`** — источник истины vs computed-only.
- **P4/P5:** Deprecation **`LemmaId`/lemma resolution** для новых карт и сокращение `lemma_*` / `CardLexicon` в proto после стабилизации.

## Prioritized backlog (порядок выполнения)

### P1 — Единое превью карточки (`CardPreview` / дубликаты / связанные карты / маркетплейс-превью)

- **Задача:** строить `sentence` / `targetWord` / `translation` / `hasAudio` (и при необходимости другие поля превью) из **`notes.field_values`** через существующие хелперы (`NoteFieldMapHelper` и т.п.), чтобы не было расхождения с полной карточкой и редактором.
- **Почему первым:** пользовательские дубликаты в Reader и связанные карточки у термина опираются на превью; при неверном маппинге — ложные «совпали / не совпали» данные.

**Готово, когда:** для новых и мигрированных заметок текст превью совпадает с тем, что видно из `note.fieldValues` в UI (Expression / Word / Translation и медиа-флаги при необходимости).

---

### P2 — Capture: явная модель назначения колоды

- **Задача:** либо добавить поддержку **`deckId`** в capture (Aggregator + VocabularyService + клиент), либо зафиксировать **только Inbox** в контракте/API-доках и не подразумевать выбор колоды в продукте.
- **Почему после P1:** продуктовое решение + несколько слоёв; не блокирует корректность текста контента в превью.

**Готово, когда:** поведение, OpenAPI/контракты и UI согласованы (один выбранный вариант).

---

### P3 — Study: `CardStudyDto` и производные поля (`sourceMeta`, `targetIndex`, лексикон)

- **Задача:** определить единственный источник истины (поля заметки); `sourceMeta` / `targetIndex` либо вычисляются при отдаче в study, либо выпадают из обязательного контракта с явной документацией.

**Готово, когда:** нет двух «конкурирующих» мест хранения одного и того же без явного derived-only правила.

---

### P4 — Леммы: отвязка от новых карточек и путь к deprecation

- **Задача:** поэтапно убрать зависимость новых потоков от **lemma** как носителя знания; сузить/удалить `ResolveForCardAsync` и в перспективе **`LemmaId` на `cards`** по миграции данных (см. `.cursor/rules/06-lingq-domain-guardrails.mdc` — term-first).

**Готово, когда:** новые карточки не требуют леммы для корректного Reader/терминов/SRS; план миграции старых строк согласован.

---

### P5 — Proto / мапперы: гигиена после стабилизации домена

- **Задача:** после P4 — obsolete-поля, при необходимости версионирование gRPC, упрощение дублирующих сообщений (`lemma_*`, `CardLexicon`, …).

**Готово, когда:** генерация клиентов и документация контрактов не вводят новых потребителей в deprecated поля без пометки.

## Agents

- `product-agent`: зафиксировать решение по **P2** (Inbox-only vs выбор колоды) и границы превью **P1**.
- `backend-agent`: P1 mapping, P2 capture, P3 study DTO, P4 lemma path, P5 proto (по очереди).
- `frontend-agent`: правки клиента/API-типов только если меняются REST контракты; иначе минимально.
- `reviewer-agent`: регрессии дубликатов, study flows, guardrails термин-first.

## Tasks

История задач: `.cursor/tasks/archive/05-note-centric-followup-backlog-2026-05-14/` (`product.md`, `frontend.md`, `backend.md`).

## Verification

- `dotnet test` по затронутым проектам (`VocabularyService.Tests`, `AggregatorService.Tests`).
- По фронту: `npx tsc --noEmit` и точечные `vitest` при изменении DTO/клиента.
- Ручная проверка: дубликаты в Reader, связанные карточки, один проход Study после P3.

## Cleanup

- [x] При взятии в работу: перенос этого файла `backlog/` → `active/`, `Status: active`.
- [x] Task-папка перенесена в `archive/` при закрытии исполняемого объёма P1–P4; **P5** вынесен в отложенную работу.
- [ ] Долговечные решения (особенно P2, P4) при необходимости — в `context/decisions/` или `Docs/` (не блокер закрытия координации).
