# Введение



Study session FSRS — JWT. Downstream: **VocabularyService.StudyService**.



# 1. Список эндпоинтов



| SR | Method | Route | gRPC |

| :--- | :--- | :--- | :--- |

| SR-AGG-STUDY-01 | POST | `/api/study/session` | StartStudySession |

| SR-AGG-STUDY-02 | GET | `/api/study/session/{id}/next` | GetNextCard |

| SR-AGG-STUDY-02 | POST | `/api/study/session/{id}/review` | SubmitReview |

| SR-AGG-STUDY-02 | POST | `/api/study/session/{id}/undo` | UndoReview |



DTO: [[03 - Карточки и обучение (Cards Study)]].



---



# SR-AGG-STUDY-01: Старт сессии: POST /api/study/session



## Общая информация



Создание study session с лимитами new/review queue.



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | [StartSessionRequestDto](../DTO/03%20-%20Карточки%20и%20обучение%20(Cards%20Study).md#dto-StartSessionRequestDto) |

| **DTO успешного ответа** | [StudySessionDto](../DTO/03%20-%20Карточки%20и%20обучение%20(Cards%20Study).md#dto-StudySessionDto) |



## Логика обработки запроса



* JWT → userId

* gRPC **`StartStudySession`**

* FSRS scheduling — inclusive/VocabularyService



## Успешный ответ



HTTP **201**, `StudySessionDto` с `queueStats`.



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **400** | Invalid project/deck scope |

| **401** | JWT |

| **502** | Downstream |



---



# SR-AGG-STUDY-02: Submit review: POST /api/study/session/{id}/review



## Общая информация



Оценка карточки (Again/Hard/Good/Easy) → FSRS update.



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | ReviewCardRequestDto |

| **DTO успешного ответа** | ReviewResponseDto |



## Логика обработки запроса



* gRPC **`SubmitReview`**

* Response: next interval, queue stats update



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **404** | Session or card not found |

| **409** | Session completed |

| **502** | Downstream |



---



# SR-AGG-STUDY-02: Undo review: POST /api/study/session/{id}/undo



## Общая информация



Откат последней оценки в рамках сессии.



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | UndoReviewRequestDto |

| **DTO успешного ответа** | UndoResponseDto |



## Логика обработки запроса



* gRPC **`UndoReview`**

* Один уровень undo (или stack depth — см. VocabularyService)



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **400** | Nothing to undo |

| **502** | Downstream |



---



# SR-AGG-STUDY-02: Next card: GET /api/study/session/{id}/next



## Общая информация



Следующая карточка из очереди new/review.



| Тип метода | GET |

| :--- | :--- |

| **DTO успешного ответа** | NextCardResponseDto |



## Параметры URL



| Название | Тип | Описание |

| :--- | :--- | :--- |

| id | guid | Study session id |



## Логика обработки запроса



* JWT → userId

* gRPC **`GetNextCard`**



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **404** | Session not found |

| **409** | Session completed |

| **502** | Downstream |



{#SR-AGG-STUDY-01}

