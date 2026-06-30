# 04 - Reader и термины (Reader)

DTO term-first модели (LingQ-style). Источник: `TermDtos.cs`, `TextAnalyzeDtos.cs`, Reader library DTOs.

## CreateOrUpdateTermDto {#dto-CreateOrUpdateTermDto}

| Поле | Тип | Обязательно | Описание |
| :--- | :--- | :---: | :--- |
| projectId | string | да | |
| termText | string | да | **Точная форма** (не lemma) |
| type | string | да | WORD / PHRASE |
| language | string | | ISO code |
| status | string | | SAVED по умолчанию |
| meaning | string? | | |
| firstSentence | string? | | Контекст |
| firstSourceTitle | string? | | |
| firstSourceUrl | string? | | |

## TermActionDto {#dto-TermActionDto}

Для MarkKnown / Ignore — `projectId`, `termText`, `type`, `language`.

## BulkMarkKnownDto {#dto-BulkMarkKnownDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| projectId | string | |
| termTexts | string[] | Legacy list |
| items | BulkMarkKnownItemDto[] | Preferred: text + type |
| language | string | |

## TermDetailsDto, ProjectTermListItemDto, SearchTermDuplicatesDto

См. `TermDtos.cs` — duplicate check по **normalized exact form**.

## TextAnalyzeRequestDto / TextAnalyzeResponseDto {#dto-TextAnalyzeResponseDto}

Reader tokenization + status overlay для подсветки. См. `Reader/TextAnalyzeDtos.cs`.

## ReaderLibraryBookDto, ReaderCollectionDto, SaveReaderLibraryBookDto

Reader Library — см. `ReaderLibraryBookDto.cs`, `ReaderCollectionDto.cs`.
