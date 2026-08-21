# Введение

Алгоритмы **Reader Books**: JSON-индекс library и обогащение ответов URL.

# 1. Список алгоритмов

| Алгоритм | SR |
| :--- | :--- |
| Чтение/запись JSON-индекса library | SR-MEDIA-BOOK-01 … 03 |
| Сортировка и hydration URL книг | SR-MEDIA-BOOK-01, SR-MEDIA-BOOK-02 |

---

# Алгоритм чтения/записи JSON-индекса library

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-BOOK-01, SR-MEDIA-BOOK-02, SR-MEDIA-BOOK-03

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | List / save / delete books в project library |
| 2 | Owner isolation по `user_id` в S3 path |

## Входные данные

| Параметр | Тип | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `userId` | `uuid` | Owner (из metadata) | Да |
| `projectId` | `string` | Project id (sanitized) | Да |
| `books` | `array` | `ReaderLibraryBookRecord` | Да (save) |

## Выходные данные

| Параметр | Тип | Описание |
| :--- | :--- | :--- |
| `books` | `array` | Sorted book records |
| `s3_key` | `string` | `reader-library/{userId}/{projectId}/index.json` |

## Логика работы (Псевдокод)

```csharp
var key = $"reader-library/{userId:D}/{Sanitize(projectId)}/index.json";
// Read: GetObject → deserialize List<ReaderLibraryBookRecord> or empty
// Save: upsert by book.Id, force OwnerUserId = userId, SortBooks, PutObject JSON
// Delete: filter out bookId, PutObject JSON
// Sanitize: trim, replace / and \ with _
```

## Связанные артефакты

* gRPC: `#grpc-ListReaderLibraryBooks`, `#grpc-SaveReaderLibraryBook`, `#grpc-DeleteReaderLibraryBook`
* КАР-2: JSON-индексы в object storage
* Entity: `ReaderLibraryBookRecord`

---

# Алгоритм сортировки и hydration URL книг

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-BOOK-01, SR-MEDIA-BOOK-02

## Логика работы (Псевдокод)

```csharp
books = books.OrderByDescending(last_opened_at ?? uploaded_at)
             .ThenBy(title, ignoreCase);
foreach (book in books where book.DocumentId != null)
    book.ResponseUrl = GetMediaUrlForServerFetch(documentId, "documents");
```

## Связанные артефакты

* gRPC: `#grpc-ListReaderLibraryBooks`, `#grpc-SaveReaderLibraryBook`
* Алгоритм: [[01 - Загрузка и выдача медиа (Media Storage)#Алгоритм резолва URL (public vs server-fetch)]]
