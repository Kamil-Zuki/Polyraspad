# Frontend Task

Plan ID: `05-vocabulary-list-2026-05-14`
Agent: `frontend-agent`
Status: pending
Can run in parallel: yes (после стабильного DTO в контракте или с mock)

## Objective

Страница **`/vocabulary`**: таблица или список терминов, фильтры, поиск debounced, пагинация (React Query `useInfiniteQuery` или page-based — как в контракте), loading/error states. Пункт навигации в `sidebar.tsx` (группа Learning).

## Inputs

- `.cursor/plans/active/05-vocabulary-list-2026-05-14.md`
- `useProjectContext`, существующие паттерны `apiClient`, `ROUTES` в `constants.ts`

## Scope

- Минимальный polished UI (design system reader/dashboard).
- Не дублировать логику нормализации — статусы с бэка отображать с маппингом цветов как в reader.

## Out of Scope

- Редактирование термина inline (открыть follow-up к плану 02 mining).

## Deliverables

- `src/app/vocabulary/page.tsx` (+ loading.tsx при необходимости)
- `term-client` метод `listProjectTerms`
- RTL тесты ключевых состояний

## Verification

- `npm test` / vitest по пути vocabulary.

## Handoff

- Скрин состояний для reviewer.
