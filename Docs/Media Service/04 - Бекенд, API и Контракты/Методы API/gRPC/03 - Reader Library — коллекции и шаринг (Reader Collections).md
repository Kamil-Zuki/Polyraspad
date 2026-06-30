# Введение

Методы группы **Reader Library — коллекции и шаринг (Reader Collections)** управляют JSON-индексом `reader-collections/{userId}/{projectId}/index.json`, collaborators snapshot и inbox расшаренных коллекций.

Share **не** копирует книги — collaborator видит owner books через scan при `ListSharedReaderCollections`.

**SR:** SR-MEDIA-COLL-01 … SR-MEDIA-COLL-06. **КАР:** [[02 - Архитектура/02 - КАР-2 - JSON-индексы Reader Library в object storage|КАР-2]].

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-MEDIA-COLL-01 | `ListReaderCollections` | Unary | Коллекции owner + nested books. |
| SR-MEDIA-COLL-02 | `SaveReaderCollection` | Unary | Upsert коллекции. |
| SR-MEDIA-COLL-03 | `DeleteReaderCollection` | Unary | Delete + cascade `collection_id` на книгах. |
| SR-MEDIA-COLL-04 | `ShareReaderCollection` | Unary | Добавление collaborator. |
| SR-MEDIA-COLL-05 | `UnshareReaderCollection` | Unary | Удаление collaborator. |
| SR-MEDIA-COLL-06 | `ListSharedReaderCollections` | Unary | Inbox shared collections. |

---

<span id="grpc-ListReaderCollections"></span>

# SR-MEDIA-COLL-01: List collections: ListReaderCollections

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Reader Library — коллекции и шаринг (Reader Collections)#SR-MEDIA-COLL-01]]

| Сигнатура | `rpc ListReaderCollections(ListReaderCollectionsRequest) returns (ListReaderCollectionsResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ListReaderCollectionsRequest` — `project_id` |
| **Сообщение ответа** | `ListReaderCollectionsResponse` — `collections[]` с nested `books[]` |

## Логика обработки запроса

1. Извлечь `user_id`, валидировать `project_id`.
2. Загрузить collections index и library books index для owner.
3. Для каждой collection — attach books где `book.collection_id == collection.id`; вычислить `book_count`.
4. Вернуть enriched `collections`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing `project_id`. |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |

---

<span id="grpc-SaveReaderCollection"></span>

# SR-MEDIA-COLL-02: Save collection: SaveReaderCollection

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Reader Library — коллекции и шаринг (Reader Collections)#SR-MEDIA-COLL-02]]

| Сигнатура | `rpc SaveReaderCollection(SaveReaderCollectionRequest) returns (SaveReaderCollectionResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `SaveReaderCollectionRequest` — `collection` (`ReaderCollection`) |
| **Сообщение ответа** | `SaveReaderCollectionResponse` — saved `collection` |

## Логика обработки запроса

1. Извлечь `user_id`; валидировать `collection` payload.
2. Валидировать `collection.id` (UUID), `collection.project_id`, `collection.name`.
3. Установить `owner_user_id = caller`; merge collaborators snapshot из payload.
4. Upsert в collections index; перезаписать S3 JSON.
5. Загрузить books, map collection с nested books, вернуть.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing collection, invalid UUIDs, missing name/project_id. |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |

---

<span id="grpc-DeleteReaderCollection"></span>

# SR-MEDIA-COLL-03: Delete collection: DeleteReaderCollection

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Reader Library — коллекции и шаринг (Reader Collections)#SR-MEDIA-COLL-03]]

| Сигнатура | `rpc DeleteReaderCollection(DeleteReaderCollectionRequest) returns (DeleteReaderCollectionResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `DeleteReaderCollectionRequest` — `project_id`, `collection_id` |
| **Сообщение ответа** | `DeleteReaderCollectionResponse` (empty) |

## Логика обработки запроса

1. Извлечь `user_id`; валидировать `project_id`, `collection_id` (UUID).
2. Удалить collection из collections index.
3. В library index: для книг с matching `collection_id` — очистить `collection_id` и `collection_name`.
4. Перезаписать оба JSON-файла в S3. Книги не удаляются.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing/invalid ids. |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |

---

<span id="grpc-ShareReaderCollection"></span>

# SR-MEDIA-COLL-04: Share collection: ShareReaderCollection

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Reader Library — коллекции и шаринг (Reader Collections)#SR-MEDIA-COLL-04]]

| Сигнатура | `rpc ShareReaderCollection(ShareReaderCollectionRequest) returns (ShareReaderCollectionResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ShareReaderCollectionRequest` — `project_id`, `collection_id`, `collaborator` |
| **Сообщение ответа** | `ShareReaderCollectionResponse` — updated `collection` |

## Логика обработки запроса

1. Извлечь `user_id`; валидировать `project_id`, `collection_id`, `collaborator.user_id` (UUID).
2. Найти collection в owner index; upsert collaborator (replace prior entry same `user_id`).
3. Bump `updated_at`; сохранить collections index.
4. Map collection с books; вернуть.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing/invalid collaborator or collection ids. |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |
| **INTERNAL** | Collection not found в owner index. |

---

<span id="grpc-UnshareReaderCollection"></span>

# SR-MEDIA-COLL-05: Unshare collection: UnshareReaderCollection

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Reader Library — коллекции и шаринг (Reader Collections)#SR-MEDIA-COLL-05]]

| Сигнатура | `rpc UnshareReaderCollection(UnshareReaderCollectionRequest) returns (UnshareReaderCollectionResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `UnshareReaderCollectionRequest` — `project_id`, `collection_id`, `collaborator_user_id` |
| **Сообщение ответа** | `UnshareReaderCollectionResponse` — updated `collection` |

## Логика обработки запроса

1. Извлечь owner `user_id`; валидировать ids.
2. Удалить collaborator из массива; bump `updated_at`.
3. Сохранить index; map и вернуть collection.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing/invalid UUIDs. |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |

---

<span id="grpc-ListSharedReaderCollections"></span>

# SR-MEDIA-COLL-06: List shared collections: ListSharedReaderCollections

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/03 - Reader Library — коллекции и шаринг (Reader Collections)#SR-MEDIA-COLL-06]]

| Сигнатура | `rpc ListSharedReaderCollections(ListSharedReaderCollectionsRequest) returns (ListSharedReaderCollectionsResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ListSharedReaderCollectionsRequest` (empty) |
| **Сообщение ответа** | `ListSharedReaderCollectionsResponse` — `collections[]` |

## Логика обработки запроса

1. Извлечь caller `user_id` из metadata.
2. List S3 prefix `reader-collections/`; для каждого `…/index.json` parse owner `userId` и `projectId` из key path.
3. Для каждой collection — если `collaborators` содержит caller — включить в результат.
4. Загрузить owner books; attach books в collection; set `is_shared_with_me=true`, `can_edit` from access record, `is_shared=true` на books.
5. Сортировать по collection name; вернуть.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |
| **UNAVAILABLE** | S3 list/read errors. |

## Ограничения

Cross-user scan — **O(n)** по числу collection indices ([[03 - Модель Данных/01 - Основные сущности/Entity - Reader Library - Reader Library#Reader Collection Collaborator]]). При росте — ISSUE на индекс/БД.
