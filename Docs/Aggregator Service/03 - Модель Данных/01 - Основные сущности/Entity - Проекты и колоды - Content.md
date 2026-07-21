# Entity - Проекты и колоды - Content

**Тип:** API Contract View

Downstream: `VocabularyService` Content/Deck APIs.

## ProjectResponse (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id | string | Project id |
| userId | string | Владелец |
| title | string | Название |
| sourceLang | string | Родной язык |
| targetLang | string | Изучаемый язык |
| settings | object? | SRS/FSRS settings DTO |
| stats | object? | `totalTerms`, `knownTerms` |
| isArchived | bool | Архив |
| createdAt | datetime | Создание |

> REST Projects: Create/Read/Update — **Delete project в контроллере отсутствует**.

## DeckResponse / DeckTreeItem (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id | string | Deck id |
| projectId | string | Проект |
| parentDeckId | string? | Родитель |
| title | string | Название |
| description | string? | Описание |
| cardCount | int | Число карточек (если отдаётся) |
| children | array? | Вложенные колоды в tree |

REST: `/api/Projects`, `/api/Decks`.
