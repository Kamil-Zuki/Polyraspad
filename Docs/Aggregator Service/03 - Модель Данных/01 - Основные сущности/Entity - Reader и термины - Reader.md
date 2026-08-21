# Entity - Reader и термины - Reader

**Тип:** API Contract View (не персистентная таблица BFF)

Downstream: `VocabularyService` — `TermService`, `TextService`.

Идентификаторы терминов на границе REST — **string** (GUID), не `int`.

## TermDetails / ProjectTermListItem (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| termId | string | Id термина (`ProjectTerm.Id`) |
| projectId | string | Id проекта |
| termText / text | string | Точная форма |
| normalizedText | string | trim + lower + схлопывание пробелов |
| type | string | `WORD` \| `PHRASE` |
| language | string | ISO code |
| status | string | `NEW`, `SAVED`, `KNOWN`, `IGNORED` (+ legacy mapping downstream) |
| meaning | string? | Перевод/толкование |
| firstSentence | string? | Контекст первого вхождения |
| firstSourceTitle | string? | Источник |
| firstSourceUrl | string? | URL источника |
| relatedCards / relatedCardCount | array / int | Связанные карточки |
| readingLevel | int | 0–4 |
| listeningLevel | int | 0–4 |
| writingLevel | int | 0–4 |
| speakingLevel | int | 0–4 |
| updatedAt | datetime | Только в list item |

## BulkMarkKnown (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| projectId | string | Проект |
| termTexts | string[] | Упрощённый список текстов |
| items | `{ termText, type }[]` | Типизированные элементы |
| language | string | Язык |

Ответ: `{ updatedCount: int }`.

## TextAnalyzeResponse (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| tokens | array | `TextTokenDto`: text, status, termId — **без** start/end index в текущем DTO |
| phrases | array | `TextPhraseDto` — приоритет подсветки над словами; offsets у фраз (если есть в DTO) |
| stats | object | счётчики new/known/saved |

REST: `/api/terms`, `/api/text/analyze`. DTO: `AggregatorService/Dtos/TermDtos.cs`, Text analyze DTOs.

См. `AGENTS.md` §13 — term-first invariants.
