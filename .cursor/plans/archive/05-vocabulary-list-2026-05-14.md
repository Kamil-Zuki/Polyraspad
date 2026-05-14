# 05 — Vocabulary: список терминов по проекту (API + UI)

Plan ID: `05-vocabulary-list-2026-05-14`
Priority: **05**
Status: archived
Created: 2026-05-14
Owner: `lead-agent`

Родительский индекс: [`00-reader-lingq-hub-2026-05-14.md`](../backlog/00-reader-lingq-hub-2026-05-14.md)

## Goal

Пользователь открывает раздел **Vocabulary** и видит **пагинируемый список терминов текущего проекта** (term-first: `ProjectTerm` + `UserTermStatus`): форма текста, тип WORD|PHRASE, статус NEW / SAVED / KNOWN / IGNORED, значение и контекст при наличии. Данные приходят с **публичного REST API** через Aggregator (Bearer), согласованного с Vocabulary gRPC.

## Out of Scope

- Редактирование карточек SRS из списка (остаётся Browser/Study).
- Массовый импорт/экспорт CSV (отдельный backlog).
- Леммы как источник истины (запрещено; см. guardrails).

## Граница MVP

- **GET** список с `projectId`, опционально `status`, `type`, `q` (поиск по `Text` / `NormalizedText`), **cursor** или `page`+`pageSize`.
- Страница **`/vocabulary`** в Next.js + пункт в сайдбаре (Learning).
- Только **свои** термины пользователя в рамках проекта (как в остальных term endpoints).

## Agents

- `product-agent`: IA списка, фильтры, пустые состояния, согласование имён статусов с reader (SAVED vs LEARNING в analyze).
- `backend-agent`: запрос к БД (join `ProjectTerms` + `UserTermStatuses`), новый gRPC на Vocabulary + маппинг; **Aggregator** `TermsController` или расширение существующего REST-моста; тесты.
- `frontend-agent`: страница, React Query, `term-client`/types, доступность таблицы.
- `reviewer-agent`: term-first регрессии, пагинация, авторизация.

## Contracts To Lock

- `Docs/api/reader-aggregator-contract.md` — секция **GET list project terms** (query params, response DTO, коды ошибок).
- `polyraspad-frontend` `ROUTES` + `constants.ts` (`API_ENDPOINTS.TERMS.LIST` или аналог).
- Статусы в JSON: **`SAVED`** для жёлтого (не путать с legacy `LINGQ` в БД); для UI можно дублировать `displayStatus` если нужно.
- Пагинация: рекомендация **cursor** (opaque) по `(UpdatedAt, Id)` для стабильности; зафиксировать в контракте.

## Tasks

- `.cursor/tasks/archive/05-vocabulary-list-2026-05-14/`

## Verification

- `dotnet test` на новых integration tests Aggregator/Vocabulary.
- `npm test` / RTL для страницы vocabulary (поиск, фильтр, пустой список).
- Ручной смоук: смена проекта → другой список; 401 без токена.

## References

- `.cursor/rules/06-lingq-domain-guardrails.mdc`
- `Docs/api/reader-aggregator-contract.md`
- `VocabularyService/Services/TermService.cs`, сущности `ProjectTerm` / `UserTermStatus`

## Execution status

| Task file | Agent | Status |
|-----------|--------|--------|
| `product.md` | product-agent | done |
| `backend.md` | backend-agent | done |
| `frontend.md` | frontend-agent | done |
| `review.md` | reviewer-agent | done |

## Cleanup

- [x] Task-папка → `archive/` по завершении
- [x] План → `plans/archive/` по завершении
