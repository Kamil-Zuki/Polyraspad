# Backend Task

Plan ID: `01-reader-mvp-read-2026-05-14`
Agent: `backend-agent`
Status: done
Can run in parallel: yes

## Completion

Реализовано: `POST /api/Media/extract-document-text`, **422 NoExtractableText**, транзакционный `BulkMarkKnown`, тесты `MediaControllerTests` / `TermServiceTests`, обновлён `Docs/api/reader-aggregator-contract.md`.

## Objective

(архив) Надёжный pipeline текста для **EPUB**, **TXT**, **paste**; для **PDF** — явный сигнал при отсутствии извлекаемого текста. **BulkMarkKnown** в одной транзакции.

## Verification

- `dotnet test AggregatorService.Tests --filter FullyQualifiedName~MediaControllerTests`
- `dotnet test VocabularyService.Tests --filter FullyQualifiedName~TermServiceTests.BulkMarkKnown`
