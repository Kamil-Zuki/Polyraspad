# 03 - Карточки и обучение (Cards Study)

DTO для Cards, Study, Analytics.

## CreateCardDto {#dto-CreateCardDto}

| Поле | Тип | Обязательно | Описание |
| :--- | :--- | :---: | :--- |
| deckId | string (guid) | да | Целевая колода |
| fieldValues | map&lt;string, NoteFieldValueDto&gt; | да | Anki-like поля note |

## CardResponseDto {#dto-CardResponseDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id | string | Card id |
| deckId | string | |
| fieldValues | map | Rendered fields |
| due | datetime? | FSRS due |

## CaptureCardDto, BulkCreateCardsDto, CheckCardDuplicatesDtos

См. `CaptureCardDto.cs`, `BulkCreateCardsDto.cs`, `CheckCardDuplicatesDtos.cs`.

## StartSessionRequestDto {#dto-StartSessionRequestDto}

| Поле | Тип | Обязательно |
| :--- | :--- | :---: |
| projectId | string | да |
| deckIds | string[]? | нет |
| maxNew | int? | нет |
| maxReview | int? | нет |

## StudySessionDto {#dto-StudySessionDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id | string | Session id |
| projectId | string | |
| status | string | ACTIVE / COMPLETED |
| startTime | datetime | |
| cardsReviewed | int | |
| queueStats | QueueStatsDto | new/review/learning counts |

## CardStudyDto, ReviewCardRequestDto, ReviewResponseDto, UndoReviewRequestDto, UndoResponseDto

См. `CardStudyDto.cs`, `ReviewCardRequestDto.cs`, `ReviewResponseDto.cs`, `UndoResponseDto.cs`.

## VocabularyStatsResponseDto, HeatmapResponseDto, DailySummaryResponseDto

Analytics dashboard DTO — см. соответствующие `.cs` в корне `Dtos/`.
