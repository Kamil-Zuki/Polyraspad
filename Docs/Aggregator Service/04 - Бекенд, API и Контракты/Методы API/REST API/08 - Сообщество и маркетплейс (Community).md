# Введение



Contributions, publish/fork, marketplace products и deck entitlement. JWT на всех маршрутах (`CommunityController`, base `/api`). Downstream: **VocabularyService** Community gRPC.



DTO: [[05 - Сообщество, биллинг и агент (Community Billing Agent)]].



# 1. Список эндпоинтов



Сверено с `AggregatorService/Controllers/CommunityController.cs`.



| SR | Method | Route | gRPC / назначение |

| :--- | :--- | :--- | :--- |

| SR-AGG-COMM-01 | POST | `/api/contributions` | CreateContribution |

| SR-AGG-COMM-01 | GET | `/api/contributions` | ListMyContributions |

| SR-AGG-COMM-01 | GET | `/api/decks/{deckId}/contributions` | ListDeckContributions |

| SR-AGG-COMM-01 | GET | `/api/contributions/{id}` | GetContribution |

| SR-AGG-COMM-01 | POST | `/api/contributions/{id}/resolve` | ResolveContribution |

| SR-AGG-COMM-01 | PUT | `/api/decks/{deckId}/contribution-policy` | UpdateContributionPolicy |

| SR-AGG-COMM-02 | POST | `/api/decks/{deckId}/publish` | PublishDeck |

| SR-AGG-COMM-02 | POST | `/api/decks/{deckId}/fork` | ForkDeck |

| SR-AGG-COMM-02 | GET | `/api/decks/published` | ListPublishedDecks |

| SR-AGG-COMM-02 | GET | `/api/authors/{authorId}` | GetAuthorProfile |

| SR-AGG-COMM-03 | POST | `/api/marketplace/products` | CreateProduct |

| SR-AGG-COMM-03 | PUT | `/api/marketplace/products/{id}` | UpdateProduct |

| SR-AGG-COMM-03 | GET | `/api/marketplace/products` | ListProducts |

| SR-AGG-COMM-03 | GET | `/api/marketplace/products/{id}` | GetProduct |

| SR-AGG-COMM-03 | POST | `/api/marketplace/products/{id}/reviews` | CreateReview |

| SR-AGG-COMM-03 | GET | `/api/marketplace/products/{id}/stats` | GetProductStats |

| SR-AGG-COMM-03 | GET | `/api/decks/{deckId}/entitlement` | GetDeckEntitlement |



---



# SR-AGG-COMM-01: Создание contribution: POST /api/contributions



## Общая информация



Предложение изменений в shared/published deck.



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | CreateContributionDto |

| **DTO успешного ответа** | ContributionResponseDto |



## Параметры URL



Параметры отсутствуют.



## Логика обработки запроса



* JWT → `user_id`, `roles` в gRPC metadata

* gRPC **`CreateContribution`** через `IVocabularyServiceClient`



## Успешный ответ



HTTP **201**, `ContributionResponseDto`.



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **400** | Invalid payload |

| **401** | JWT |

| **403** | Not allowed |

| **404** | Deck not found |

| **502** | Downstream |



---



# SR-AGG-COMM-01: Resolve contribution: POST /api/contributions/{id}/resolve



## Общая информация



Approve/reject предложения (maintainer workflow).



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | ResolveContributionDto |

| **DTO успешного ответа** | ContributionResponseDto |



## Параметры URL



| Название | Тип | Описание |

| :--- | :--- | :--- |

| id | guid | Id contribution |



## Логика обработки запроса



* gRPC **`ResolveContribution`**



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **403** | Not maintainer |

| **404** | Contribution not found |

| **502** | Downstream |



---



# SR-AGG-COMM-02: Публикация колоды: POST /api/decks/{deckId}/publish



## Общая информация



Перевод private deck в published catalog.



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | PublishDeckDto |

| **DTO успешного ответа** | PublishedDeckResponseDto |



## Параметры URL



| Название | Тип | Описание |

| :--- | :--- | :--- |

| deckId | guid | Id колоды |



## Логика обработки запроса



* gRPC **`PublishDeck`**



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **403** | Not owner |

| **409** | Already published |

| **502** | Downstream |



---



# SR-AGG-COMM-03: Каталог products: GET /api/marketplace/products



## Общая информация



Пагинированный список marketplace products (не `/marketplace/decks`).



| Тип метода | GET |

| :--- | :--- |

| **DTO запроса** | Query: filters, page |

| **DTO успешного ответа** | ProductResponseDto[] / paginated |



## Логика обработки запроса



* gRPC list products на CommunityService



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **502** | Downstream |



---



# SR-AGG-COMM-03: Deck entitlement: GET /api/decks/{deckId}/entitlement



## Общая информация



Проверка premium access к deck **до** открытия контента (marketplace entitlement, не SaaS Billing).



| Тип метода | GET |

| :--- | :--- |

| **DTO запроса** | N/A |

| **DTO успешного ответа** | EntitlementDto |



## Параметры URL



| Название | Тип | Описание |

| :--- | :--- | :--- |

| deckId | guid | Id колоды |



## Логика обработки запроса



* JWT → userId

* gRPC **`GetDeckEntitlement`**



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **404** | Deck not found |

| **502** | Downstream |



---

# SR-AGG-COMM-01: List my contributions: GET /api/contributions

## Общая информация

| Тип метода | GET |

## Логика обработки запроса

* gRPC **`GetContributions`** (author filter)

---

# SR-AGG-COMM-01: List deck contributions: GET /api/decks/{deckId}/contributions

## Общая информация

| Тип метода | GET |

## Логика обработки запроса

* gRPC **`GetContributions`**

---

# SR-AGG-COMM-01: Get contribution: GET /api/contributions/{id}

## Общая информация

| Тип метода | GET |

## Логика обработки запроса

* gRPC **`GetContribution`**

---

# SR-AGG-COMM-01: Contribution policy: PUT /api/decks/{deckId}/contribution-policy

## Общая информация

| Тип метода | PUT |

## Логика обработки запроса

* gRPC **`UpdateContributionPolicy`**

---

# SR-AGG-COMM-02: Fork deck: POST /api/decks/{deckId}/fork

## Общая информация

| Тип метода | POST |

## Логика обработки запроса

* gRPC **`ForkDeck`**

---

# SR-AGG-COMM-02: Published catalog: GET /api/decks/published

## Общая информация

| Тип метода | GET |

## Логика обработки запроса

* gRPC **`GetPublishedDecks`**

---

# SR-AGG-COMM-02: Author profile: GET /api/authors/{authorId}

## Общая информация

| Тип метода | GET |

## Логика обработки запроса

* gRPC **`GetAuthorProfile`**

---

# SR-AGG-COMM-03: Create product: POST /api/marketplace/products

## Общая информация

| Тип метода | POST |

## Логика обработки запроса

* gRPC **`CreateProduct`**

---

# SR-AGG-COMM-03: Update product: PUT /api/marketplace/products/{id}

## Общая информация

| Тип метода | PUT |

## Логика обработки запроса

* gRPC **`UpdateProduct`**

---

# SR-AGG-COMM-03: Product details: GET /api/marketplace/products/{id}

## Общая информация

| Тип метода | GET |

## Логика обработки запроса

* gRPC **`GetProductDetails`**

---

# SR-AGG-COMM-03: Create review: POST /api/marketplace/products/{id}/reviews

## Общая информация

| Тип метода | POST |

## Логика обработки запроса

* gRPC **`CreateReview`**

---

# SR-AGG-COMM-03: Product stats: GET /api/marketplace/products/{id}/stats

## Общая информация

| Тип метода | GET |

## Логика обработки запроса

* gRPC **`GetProductStats`**
