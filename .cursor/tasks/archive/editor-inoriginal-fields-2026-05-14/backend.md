# Backend Task

Plan ID: `editor-inoriginal-fields-2026-05-14`
Agent: `backend-agent`
Status: done
Can run in parallel: no (после product-контракта персистентности)

## Objective

При утверждённом product-решении о хранении расширить модель карточки и REST/gRPC контракты так, чтобы новые поля редактора сохранялись и отдавались при GET карточки; сохранить обратную совместимость для существующих записей.

## Inputs

- Plan и locked contract из `product-agent`
- Точки входа: Aggregator card endpoints и downstream card service (как в репозитории заведено)
- Frontend types: `CreateCardDto`, `UpdateCardDto`, `CardResponseDto`

## Scope

- Неразрушающая миграция (nullable или default), заполнение при чтении старых карточек.
- DTO и маппинг; тесты на create/update/get.

## Out of Scope

- Изменения vocabulary/term pipeline, не требуемые для хранения текстовых полей карточки.

## Deliverables

- Реализация + тесты; краткий список изменённых endpoint/DTO для frontend.

## Verification

- `dotnet test` (узкий фильтр по затронутым проектам).

## Handoff

- Финальная форма JSON полей для синхронизации `types.ts` и `EditorForm`.
