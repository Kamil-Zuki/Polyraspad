# Entity - Reader и термины - Reader

**Тип:** API Contract View (не персистентная таблица BFF)

Downstream: `VocabularyService` — `TermService`, `TextService`.

## ProjectTerm (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id | int | ProjectTermId |
| text | string | Точная форма |
| normalizedText | string | trim + lowercase |
| type | enum | WORD \| PHRASE |
| language | string | ISO code, default en |
| status | enum | NEW, SAVED, KNOWN, IGNORED |

## UserTermStatus (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| meaning | string? | Для SAVED |
| firstSentence | string? | Контекст первого вхождения |
| lastSeenAt | datetime? | |

## TextAnalyzeResponse (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| tokens | array | TextTokenDto с termId, status, offsets |
| phrases | array | TextPhraseDto — приоритет над word highlight |
| stats | object | new/known counts |

REST: `/api/terms`, `/api/text/analyze`. DTO: `04/.../DTO/04 - Reader`.

См. `.cursor/rules/06-lingq-domain-guardrails.mdc` — term-first invariants.
