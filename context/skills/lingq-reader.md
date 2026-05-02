# Skill: LingQ Reader Work

Use this skill for Library, Reader, vocabulary status, duplicate checks, and reader-created cards.

## First Reads

- `context/plans/active/lingq-reader-implementation-plan.md`
- `context/rules/frontend-rules.md`
- `context/rules/backend-rules.md`
- `context/rules/testing-rules.md`

## Core Rules

- Real forms and phrases are the learning units.
- Lemmas are not used for status, duplicate checks, statistics, or card creation.
- Reader actions should happen inside reader.
- Phrase terms have priority over word terms during highlighting.

## Files To Inspect

- `polyraspad-frontend/src/app/reader/page.tsx`
- `polyraspad-frontend/src/app/reader/reader-utils.ts`
- `VocabularyService/Services/TextService.cs`
- `VocabularyService/Services/CardService.cs`
- `AggregatorService/Controllers/CardsController.cs`
- `polyraspad-frontend/src/lib/api/types.ts`
