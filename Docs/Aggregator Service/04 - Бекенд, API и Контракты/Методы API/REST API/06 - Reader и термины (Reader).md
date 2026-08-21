# Введение

REST-мост к `TermService` и `TextService` (VocabularyService gRPC). Все маршруты требуют JWT. Term-first model: duplicate check по exact normalized form, не по lemma.

DTO: [[04 - Reader и термины (Reader Terms)]].

# 1. Список эндпоинтов

| SR | Method | Endpoint | gRPC |
| :--- | :--- | :--- | :--- |
| SR-AGG-READER-01 | GET | `/api/terms?projectId=` | ListProjectTerms |
| SR-AGG-READER-01 | POST | `/api/terms` | CreateOrUpdateTerm |
| SR-AGG-READER-01 | POST | `/api/terms/mark-known` | MarkTermKnown |
| SR-AGG-READER-01 | POST | `/api/terms/ignore` | IgnoreTerm |
| SR-AGG-READER-01 | POST | `/api/terms/bulk-known` | BulkMarkKnown |
| SR-AGG-READER-03 | GET | `/api/terms/details` | GetTermDetails |
| SR-AGG-READER-03 | POST | `/api/terms/search-duplicates` | SearchTermDuplicates |
| SR-AGG-READER-03 | POST | `/api/terms/purge-demo-import` | PurgeDemoImport |
| SR-AGG-READER-02 | POST | `/api/text/analyze` | AnalyzeText |

---

# SR-AGG-READER-01: CreateOrUpdateTerm: POST /api/terms

## Общая информация

Создание или обновление SAVED термина (жёлтый LingQ) по **exact form**.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | CreateOrUpdateTermDto |
| **DTO успешного ответа** | TermDetailsDto |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* JWT → user_id, roles в gRPC metadata
* TermGrpcMapper → gRPC request
* gRPC **CreateOrUpdateTerm**

## Успешный ответ

HTTP **200**, `TermDetailsDto`.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing projectId/text |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-READER-01: Mark known: POST /api/terms/mark-known

## Общая информация

Перевод term в статус KNOWN (без создания карточки).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | MarkTermKnownDto |
| **DTO успешного ответа** | Status response |

## Логика обработки запроса

* gRPC **MarkTermKnown**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Invalid payload |
| **502** | Downstream |

---

# SR-AGG-READER-01: Bulk mark known: POST /api/terms/bulk-known

## Общая информация

Page-turn batch: список term IDs → KNOWN (настройка на frontend).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | BulkMarkKnownDto |
| **DTO успешного ответа** | BulkMarkKnownResponseDto |

## Логика обработки запроса

* gRPC **BulkMarkKnown** (single transaction downstream)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Empty list |
| **502** | Downstream |

---

# SR-AGG-READER-02: AnalyzeText: POST /api/text/analyze

## Общая информация

Токенизация текста с term statuses для Reader UI.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | TextAnalyzeRequestDto |
| **DTO успешного ответа** | TextAnalyzeResponseDto |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* Validate text length ≤ 100 000
* gRPC **AnalyzeText** на TextService

## Успешный ответ

HTTP **200**, tokens with status highlights.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Too long / missing projectId |
| **502** | Downstream |

---

# SR-AGG-READER-03: Get term details: GET /api/terms/details

## Общая информация

Inspector: meaning, first context, card link.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | Query: `projectId`, `termId` or text |
| **DTO успешного ответа** | TermDetailsDto |

## Логика обработки запроса

* gRPC **GetTermDetails**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Term not found |
| **502** | Downstream |

---

# SR-AGG-READER-01: List terms: GET /api/terms

## Общая информация

Список терминов project с фильтрами status/search (Reader vocabulary page).

| Тип метода | GET |
| :--- | :--- |
| **Query** | `projectId` (required), optional status, search, paging |
| **DTO успешного ответа** | paginated term list |

## Логика обработки запроса

* gRPC **`ListProjectTerms`**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing projectId |
| **502** | Downstream |

---

# SR-AGG-READER-01: Ignore term: POST /api/terms/ignore

## Общая информация

Пометка термина IGNORED (не подсвечивается в Reader).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | TermActionDto |
| **DTO успешного ответа** | TermDetailsDto |

## Логика обработки запроса

* gRPC **`IgnoreTerm`**

---

# SR-AGG-READER-03: Search duplicates: POST /api/terms/search-duplicates

## Общая информация

Проверка дубликатов по **normalized exact form** (term-first, не lemma).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | search text + projectId |

## Логика обработки запроса

* gRPC **`SearchTermDuplicates`**

---

# SR-AGG-READER-03: Purge demo import: POST /api/terms/purge-demo-import

## Общая информация

Cleanup demo import batch для project (dev/onboarding).

| Тип метода | POST |

## Логика обработки запроса

* gRPC **`PurgeDemoImport`**
* HTTP **200** с count removed
