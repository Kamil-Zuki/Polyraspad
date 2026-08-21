# Введение

Подписки текущего пользователя на published/shared decks. JWT. Downstream: **VocabularyService.SubscriptionService**.

# 1. Список эндпоинтов

| SR | Method | Route | gRPC |
| :--- | :--- | :--- | :--- |
| SR-AGG-SUB-01 | GET | `/api/subscriptions` | ListSubscriptions |
| SR-AGG-SUB-01 | POST | `/api/subscriptions/{deckId}` | Subscribe |
| SR-AGG-SUB-01 | DELETE | `/api/subscriptions/{deckId}` | Unsubscribe |

DTO: [DeckSubscriptionDto](../DTO/02%20-%20Проекты,%20колоды%20и%20контент%20(Content).md#dto-DeckSubscriptionDto).

---

# SR-AGG-SUB-01: List subscriptions: GET /api/subscriptions

## Общая информация

Список deck subscriptions текущего пользователя.

| Тип метода | GET |
| **DTO успешного ответа** | DeckSubscriptionDto[] |

## Логика обработки запроса

* JWT → userId
* gRPC **`ListSubscriptions`**

## Ошибки

| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-SUB-01: Подписка на колоду: POST /api/subscriptions/{deckId}

## Общая информация

Оформление follow на published deck. `userId` только из JWT.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | N/A (deckId в path) |
| **DTO успешного ответа** | DeckSubscriptionDto |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| deckId | guid | Id опубликованной колоды |

## Логика обработки запроса

* `MappingHelper.GetUserId` из JWT
* gRPC **`Subscribe`** с userId + deckId
* Проверка published/entitlement — в VocabularyService

## Успешный ответ

HTTP **200** или **201**, `DeckSubscriptionDto`.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | JWT |
| **404** | Deck not found / not published |
| **409** | Already subscribed |
| **502** | Downstream |

---

# SR-AGG-SUB-01: Отмена подписки: DELETE /api/subscriptions/{deckId}

## Общая информация

Снятие follow. Успех — **204 No Content**.

| Тип метода | DELETE |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | N/A |

## Логика обработки запроса

* gRPC **`Unsubscribe`**
* HTTP **204** при успехе

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Subscription not found |
| **502** | Downstream |
