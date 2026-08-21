# Введение

Projects и Decks — JWT. Downstream: `ContentService`.

# 1. Список эндпоинтов

## ProjectsController — `/api/Projects`

| SR | Method | Route | gRPC |
| :--- | :--- | :--- | :--- |
| SR-AGG-CONTENT-01 | POST | `/` | CreateProject |
| SR-AGG-CONTENT-01 | GET | `/` | GetProjects |
| SR-AGG-CONTENT-01 | GET | `/{id}` | GetProjectDetails |
| SR-AGG-CONTENT-01 | PUT | `/{id}` | UpdateProject |
| SR-AGG-CONTENT-01 | GET | `/{projectId}/decks/tree` | GetDeckTree |

## DecksController — `/api/Decks`

| SR | Method | Route | gRPC |
| :--- | :--- | :--- | :--- |
| SR-AGG-CONTENT-02 | GET | `/tree/{projectId}` | GetDeckTree |
| SR-AGG-CONTENT-02 | POST | `/` | CreateDeck |
| SR-AGG-CONTENT-02 | PUT | `/{id}` | UpdateDeck |
| SR-AGG-CONTENT-02 | DELETE | `/{id}` | DeleteDeck |
| SR-AGG-CONTENT-02 | GET | `/{id}` | GetDeckDetail |

DTO: [[02 - Проекты, колоды и контент (Content)]]. Настройки пользователя — отдельно: [[15 - Настройки пользователя (Settings)]].

---

# SR-AGG-CONTENT-01: Создание проекта: POST /api/Projects

## Общая информация

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | [CreateProjectDto](../DTO/02%20-%20Проекты,%20колоды%20и%20контент%20(Content).md#dto-CreateProjectDto) |
| **DTO успешного ответа** | ProjectResponseDto |

## Логика обработки запроса

* JWT → metadata
* gRPC **`CreateProject`**
* HTTP **201** + Location header

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Validation |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-CONTENT-02: Детали колоды: GET /api/Decks/{id}

## Общая информация

| Тип метода | GET |
| :--- | :--- |
| **DTO успешного ответа** | DeckDetailDto |

## Логика обработки запроса

* gRPC **`GetDeckDetail`**
* Includes card counts, tree position

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Deck not found |
| **403** | No access |
