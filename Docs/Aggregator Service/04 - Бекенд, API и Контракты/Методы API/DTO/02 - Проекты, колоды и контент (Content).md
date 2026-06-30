# 02 - Проекты, колоды и контент (Content)

DTO для Projects, Decks, Subscriptions. Источник: `AggregatorService/Dtos/`.

## CreateProjectDto {#dto-CreateProjectDto}

| Поле | Тип | Обязательно | Описание |
| :--- | :--- | :---: | :--- |
| title | string | да | Название (1–200) |
| sourceLang | string | да | ISO 639-1 родной язык |
| targetLang | string | да | ISO 639-1 целевой язык |
| settings | SrsSettingsDto? | нет | FSRS/SRS overrides |

## ProjectResponseDto {#dto-ProjectResponseDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id | string (guid) | Id проекта |
| title | string | |
| sourceLang | string | |
| targetLang | string | |
| createdAt | datetime | |

## CreateDeckDto / UpdateDeckDto / DeckResponseDto / DeckDetailDto / DeckTreeItemDto

См. `CreateDeckDto.cs`, `DeckResponseDto.cs`, `DeckDetailDto.cs`, `DeckTreeItemDto.cs` — поля 1:1 с vocabulary.proto Content messages.

## DeckSubscriptionDto {#dto-DeckSubscriptionDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| deckId | string | |
| userId | string | |
| subscribedAt | datetime | |

## PaginatedResponseDto&lt;T&gt; {#dto-PaginatedResponseDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| items | T[] | Страница данных |
| pageNumber | int | |
| pageSize | int | |
| totalCount | int? | Если доступно |
