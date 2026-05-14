# Editor: поля как в InOriginal Capture

Plan ID: `editor-inoriginal-fields-2026-05-14`
Status: archived
Created: 2026-05-14
Closed: 2026-05-14
Owner: `lead-agent`

## Goal

Привести страницу `/editor` к набору и структуре полей, совместимой с боковой панелью расширения **InOriginal Capture** (`inoriginal-capture-extension`): помимо предложения и целевого слова — отдельные поля **Expression** (≈ sentence), **Word**, **Transcription**, **Word Types**, **Translation**, **Definition**, **Example / Context**, **Synonyms**, **Antonyms**, **Source**, **Url**, с UX-паттернами «клик по слову в Expression», «Use selected word», «Define word» где это уместно для PVS.

## Outcome (closed)

- **Persistence:** `Card.lexicon` (JSON) + `synonyms` на gRPC/REST; миграция `AddLexiconToCards`.
- **Backend:** `CardLexicon` в proto; Create/Update/Get карточки с lexicon и synonyms; Aggregator DTO и маппинг.
- **Frontend:** `EditorCardContext`, `editor-form` (сетка полей + word picker), гидратация, `card-preview`; типы в `types.ts`.
- **Tests:** `npm test` (frontend suite), `dotnet test` — `CardService*`, `CardsController`.

## Out of Scope

- Полный перенос Chrome-only функций: захват субтитров, скриншоты таба, запись аудио с таймлайном VTT, отправка в Anki, Smart Send / waveform — не цель этого плана (если не решено product иное).
- Дублирование **Anki field mapping** UI (выпадающие привязки к полям шаблона Anki) — в PVS колоды/шаблоны другие; при необходимости отдельная задача.

## Agents

- `product-agent`: зафиксировать, какие поля обязательны на карточке PVS, куда сохраняются «словарные» поля (отдельные колонки vs агрегация в `notes`/JSON), совместимость с term-first и Reader.
- `backend-agent`: при решении product о персистентности — расширить Card/CreateCard/UpdateCard и контракты Aggregator; миграции без потери данных.
- `frontend-agent`: разметка и состояние формы, связка с `EditorCardContext`, API client, превью карточки, тесты.
- `reviewer-agent`: регрессии create/update карточки, дубли, типы.

## Contracts To Lock

- Соответствие полей UI ↔ **CreateCardDto / UpdateCardDto / CardResponseDto** (и gRPC/сервис карт, если карточки живут не только в REST DTO).
- Маппинг с **SourceMetaDto** (`title`/`url` vs отдельные Source/Url в черновике расширения).
- Поведение **notes** сегодня: либо заменяется структурой полей, либо словарные поля склеиваются в `notes` для обратной совместимости без миграции.

## Tasks

- Archive: `.cursor/tasks/archive/editor-inoriginal-fields-2026-05-14/`

## Verification

- `npm test` в `polyraspad-frontend`.
- `dotnet test` для затронутых тестовых проектов карточек/Aggregator.
- Ручная проверка: создание и редактирование карточки с новыми полями; `?cardId=` без регресса.

## Cleanup

- [x] Перенос `backlog/` → `active/` при старте работы (выполнено при закрытии)
- [x] Task-папка → `archive/` после закрытия
- [x] План → `.cursor/plans/archive/`
- [ ] При необходимости: durable decisions в `context/decisions/` или `Docs/` (опционально)

## Reference (extension)

Референс сетки полей: `inoriginal-capture-extension/src/ui/CaptureApp.tsx` (секции `editor-grid`, `SentenceDraft` в `inoriginal-capture-extension/src/shared/types.ts`).
