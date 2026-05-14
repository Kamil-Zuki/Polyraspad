# Backend Task

Plan ID: `05-vocabulary-list-2026-05-14`
Agent: `backend-agent`
Status: pending
Can run in parallel: yes (после черновика контракта с product — можно параллельно с frontend по mock DTO)

## Objective

Реализовать **список терминов проекта для пользователя**: запрос к EF, фильтры, сортировка, пагинация; **gRPC** в `vocabulary.proto` + `TermGrpcService`; **REST** в Aggregator (`GET /api/terms` или `GET /api/projects/{id}/terms` — зафиксировать в контракте).

## Inputs

- `VocabularyService` entities `ProjectTerm`, `UserTermStatus`
- `AggregatorService/Services/VocabularyServiceClient.cs` (typed client)
- `Docs/api/reader-aggregator-contract.md`

## Scope

- Только данные пользователя + `projectId` ownership check (как в других term методах).
- Индексы: при необходимости `(ProjectId, NormalizedText)` уже есть — проверить план запроса для `q`.

## Out of Scope

- Запись из списка (bulk delete) — не MVP.

## Deliverables

- Proto + migrations не требуются если только read; при новых полях в ответе — только DTO.
- Integration tests: пустой проект; смешанные статусы; фильтр `status=SAVED`.

## Verification

- `dotnet test` на затронутых test-проектах.

## Handoff

- Финальный URL + пример JSON ответа для `frontend-agent`.
