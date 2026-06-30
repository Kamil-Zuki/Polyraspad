# Введение

Методы группы **Reader Library — книги (Reader Books)** управляют метаданными книг в JSON-индексе `reader-library/{userId}/{projectId}/index.json`. Все RPC требуют gRPC metadata `user_id` ([[04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-02|SR-MEDIA-OPS-02]]).

Бинарный файл книги — отдельный Media Object в `documents/`; delete book **не** удаляет blob.

**SR:** SR-MEDIA-BOOK-01 … SR-MEDIA-BOOK-03. **КАР:** [[02 - Архитектура/02 - КАР-2 - JSON-индексы Reader Library в object storage|КАР-2]].

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-MEDIA-BOOK-01 | `ListReaderLibraryBooks` | Unary | Список книг owner в project. |
| SR-MEDIA-BOOK-02 | `SaveReaderLibraryBook` | Unary | Upsert метаданных книги. |
| SR-MEDIA-BOOK-03 | `DeleteReaderLibraryBook` | Unary | Удаление из JSON-индекса. |

---

<span id="grpc-ListReaderLibraryBooks"></span>

# SR-MEDIA-BOOK-01: List library books: ListReaderLibraryBooks

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - Reader Library — книги (Reader Books)#SR-MEDIA-BOOK-01]]

| Сигнатура | `rpc ListReaderLibraryBooks(ListReaderLibraryBooksRequest) returns (ListReaderLibraryBooksResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ListReaderLibraryBooksRequest` — `project_id` |
| **Сообщение ответа** | `ListReaderLibraryBooksResponse` — `books[]` (`ReaderLibraryBook`) |

## Логика обработки запроса

1. Извлечь и валидировать `user_id` из metadata (иначе `UNAUTHENTICATED`).
2. Валидировать непустой `project_id` (иначе `INVALID_ARGUMENT`).
3. Прочитать JSON `reader-library/{userId}/{projectId}/index.json` из S3 (пустой список если ключ отсутствует).
4. Сортировать: `last_opened_at` / `uploaded_at` desc, затем `title`.
5. Для каждой книги с `document_id` — hydrate `url` через `GetMediaUrlForServerFetchAsync`.
6. Вернуть `books`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing `project_id`. |
| **UNAUTHENTICATED** | Missing/invalid `user_id` header. |
| **UNAVAILABLE** | S3 read error. |

---

<span id="grpc-SaveReaderLibraryBook"></span>

# SR-MEDIA-BOOK-02: Save library book: SaveReaderLibraryBook

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - Reader Library — книги (Reader Books)#SR-MEDIA-BOOK-02]]

| Сигнатура | `rpc SaveReaderLibraryBook(SaveReaderLibraryBookRequest) returns (SaveReaderLibraryBookResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `SaveReaderLibraryBookRequest` — `project_id`, `book` (`ReaderLibraryBook`) |
| **Сообщение ответа** | `SaveReaderLibraryBookResponse` — `book` с resolved `url` |

## Логика обработки запроса

1. Извлечь `user_id` из metadata.
2. Валидировать `project_id`, наличие `book` payload.
3. Валидировать `book.id` (UUID), `book.title`, `book.file_name`; optional `document_id` как UUID.
4. Принудительно установить `owner_user_id = caller user_id`.
5. Upsert в список книг индекса; пересортировать.
6. Перезаписать `index.json` в S3 (`PutObject`, content-type `application/json`).
7. Map saved book с resolved `url`, вернуть в ответе.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing book, invalid UUIDs, missing title/file_name. |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |
| **UNAVAILABLE** | S3 write error. |

---

<span id="grpc-DeleteReaderLibraryBook"></span>

# SR-MEDIA-BOOK-03: Delete library book: DeleteReaderLibraryBook

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - Reader Library — книги (Reader Books)#SR-MEDIA-BOOK-03]]

| Сигнатура | `rpc DeleteReaderLibraryBook(DeleteReaderLibraryBookRequest) returns (DeleteReaderLibraryBookResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `DeleteReaderLibraryBookRequest` — `project_id`, `book_id` |
| **Сообщение ответа** | `DeleteReaderLibraryBookResponse` (empty) |

## Логика обработки запроса

1. Извлечь `user_id`, валидировать `project_id` и `book_id` (UUID).
2. Загрузить индекс, удалить записи с matching `book_id`.
3. Перезаписать `index.json`.
4. Blob `documents/{document_id}` **не** удаляется.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing/invalid `book_id` или `project_id`. |
| **UNAUTHENTICATED** | Missing/invalid `user_id`. |
