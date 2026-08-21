# Введение

Создание, чтение, обновление карточек, поиск, capture, bulk import и схема note type для Card Editor. **DELETE карточки на BFF не экспонирован** (в Vocabulary gRPC `DeleteCard` есть, REST Aggregator — нет). Все маршруты требуют **JWT**. Downstream: **VocabularyService.CardService** (`vocabulary.proto`).

# 1. Список эндпоинтов

| SR | Method | Route | gRPC |
| :--- | :--- | :--- | :--- |
| SR-AGG-CARD-01 | POST | `/api/Cards` | CreateCard |
| SR-AGG-CARD-01 | GET | `/api/Cards/{id}` | GetCard |
| SR-AGG-CARD-01 | PUT | `/api/Cards/{id}` | UpdateCard |
| SR-AGG-CARD-02 | GET | `/api/Cards/search` | SearchCards |
| SR-AGG-CARD-02 | POST | `/api/Cards/check-duplicates` | CheckCardDuplicates |
| SR-AGG-CARD-02 | POST | `/api/Cards/capture` | CaptureCard |
| SR-AGG-CARD-02 | POST | `/api/Cards/import` | BulkCreateCards |
| SR-AGG-CARD-03 | GET | `/api/Cards/note-type/editor` | GetNoteTypeForEditor |

DTO: [[03 - Карточки и обучение (Cards Study)]].

**Не проброшено на BFF (есть в `vocabulary.proto`, нет в `CardsController`):** `DeleteCard`, `SuspendCard`, `UnsuspendCard`, `GetCardsByDeck` — удаление/ suspend только через VocabularyService gRPC или будущий REST.

---

# SR-AGG-CARD-01: Создание карточки: POST /api/Cards

## Общая информация

Создание note/card в колоде через Card Editor или API.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | [CreateCardDto](../DTO/03%20-%20Карточки%20и%20обучение%20(Cards%20Study).md#dto-CreateCardDto) |
| **DTO успешного ответа** | [CardResponseDto](../DTO/03%20-%20Карточки%20и%20обучение%20(Cards%20Study).md#dto-CardResponseDto) |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* JWT → `user_id`, `roles` в gRPC metadata
* BFF маппит JSON → `CreateCardRequest`
* gRPC **`CreateCard`** на CardService
* FSRS-состояние и валидация полей note type — на стороне VocabularyService

## Успешный ответ

HTTP **201**, тело `CardResponseDto`.

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | InvalidArgument — невалидные поля note |
| **401 Unauthorized** | JWT отсутствует или невалиден |
| **403 Forbidden** | PermissionDenied — нет доступа к deck/project |
| **502 Bad Gateway** | Downstream недоступен |

---

# SR-AGG-CARD-02: Поиск карточек: GET /api/Cards/search

## Общая информация

Full-text search по карточкам project с pagination.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | Query: `query`, `projectId`, `pageNumber`, `pageSize` |
| **DTO успешного ответа** | `PaginatedResponseDto<CardResponseDto>` |

## Логика обработки запроса

* Query validation: `projectId` обязателен
* gRPC **`SearchCards`**
* Маппинг protobuf → JSON pagination DTO

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing projectId или query |
| **401** | JWT |
| **502** | Vocabulary error |

---

# SR-AGG-CARD-03: Note type для редактора: GET /api/Cards/note-type/editor

## Общая информация

Динамическая схема полей и templates для Card Editor по project.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | Query: `projectId` (guid) |
| **DTO успешного ответа** | NoteEditorSchemaDto |

## Логика обработки запроса

* gRPC **`GetNoteTypeForEditor`**
* Frontend рендерит поля без hardcoded schema

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | projectId missing |
| **404** | Project/deck not found |
| **502** | Downstream |

---

# SR-AGG-CARD-01: Get card: GET /api/Cards/{id}

## Общая информация

| Тип метода | GET |
| **DTO успешного ответа** | CardResponseDto |

## Логика обработки запроса

* gRPC **`GetCard`**

## Ошибки

| **404** | Card not found |
| **502** | Downstream |

---

# SR-AGG-CARD-01: Update card: PUT /api/Cards/{id}

## Общая информация

| Тип метода | PUT |
| **DTO запроса** | UpdateCardDto |

## Логика обработки запроса

* gRPC **`UpdateCard`**

---

# SR-AGG-CARD-02: Check duplicates: POST /api/Cards/check-duplicates

## Общая информация

Pre-save duplicate check.

## Логика обработки запроса

* gRPC **`CheckCardDuplicates`**

---

# SR-AGG-CARD-02: Capture card: POST /api/Cards/capture

## Общая информация

Extension/Reader capture → card.

## Логика обработки запроса

* gRPC **`CaptureCard`**

---

# SR-AGG-CARD-02: Bulk import: POST /api/Cards/import

## Общая информация

Bulk create from import payload.

## Логика обработки запроса

* gRPC **`BulkCreateCards`**

---

# SR-AGG-CARD-01: Get card: GET /api/Cards/{id}

## Общая информация

| Тип метода | GET |
| **DTO успешного ответа** | CardResponseDto |

## Логика обработки запроса

* gRPC **`GetCard`**

## Ошибки

| **404** | Card not found |
| **502** | Downstream |

---

# SR-AGG-CARD-01: Update card: PUT /api/Cards/{id}

## Общая информация

| Тип метода | PUT |
| **DTO запроса** | UpdateCardDto |

## Логика обработки запроса

* gRPC **`UpdateCard`**

---

# SR-AGG-CARD-02: Check duplicates: POST /api/Cards/check-duplicates

## Общая информация

Pre-save duplicate check по note fields / target word.

## Логика обработки запроса

* gRPC **`CheckCardDuplicates`**

---

# SR-AGG-CARD-02: Capture card: POST /api/Cards/capture

## Общая информация

Browser extension / Reader capture flow → card draft.

## Логика обработки запроса

* gRPC **`CaptureCard`**

---

# SR-AGG-CARD-02: Bulk import: POST /api/Cards/import

## Общая информация

CSV/Anki-style bulk create.

## Логика обработки запроса

* gRPC **`BulkCreateCards`**
* HTTP **200** с counts created/skipped
