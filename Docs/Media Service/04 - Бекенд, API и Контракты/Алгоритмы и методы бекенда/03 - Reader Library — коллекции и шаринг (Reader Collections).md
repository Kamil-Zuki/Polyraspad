# Введение

Алгоритмы **Reader Collections**: JSON-индекс коллекций, share snapshot, cross-user scan inbox.

# 1. Список алгоритмов

| Алгоритм | SR |
| :--- | :--- |
| CRUD JSON-индекса collections | SR-MEDIA-COLL-01 … 03 |
| Scan shared collections inbox | SR-MEDIA-COLL-06 |
| Cascade clear `collection_id` на книгах | SR-MEDIA-COLL-03 |

---

# Алгоритм CRUD JSON-индекса collections

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-COLL-01, SR-MEDIA-COLL-02, SR-MEDIA-COLL-04, SR-MEDIA-COLL-05

## Входные данные

| Параметр | Тип | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `userId` | `uuid` | Owner | Да |
| `projectId` | `string` | Project | Да |
| `collection` | object | `ReaderCollectionRecord` | Да (save/share) |

## Выходные данные

| Параметр | Тип | Описание |
| :--- | :--- | :--- |
| `s3_key` | `string` | `reader-collections/{userId}/{projectId}/index.json` |

## Логика работы (Псевдокод)

```csharp
var key = $"reader-collections/{userId:D}/{Sanitize(projectId)}/index.json";
// List: read JSON, join books from library index by collection_id
// Save: upsert by collection.Id, owner = userId, SortCollections by name
// Share: upsert collaborator in array, bump updated_at
// Unshare: remove collaborator by user_id
```

## Связанные артефакты

* gRPC: `#grpc-ListReaderCollections`, `#grpc-SaveReaderCollection`, `#grpc-ShareReaderCollection`
* КАР-2

---

# Алгоритм scan shared collections inbox

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-COLL-06

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | O(n) по числу collection indices в bucket |

## Логика работы (Псевдокод)

```csharp
var keys = ListObjects("reader-collections/").Where(endsWith "index.json");
foreach (key in keys) {
  parse ownerUserId, projectId from path segments;
  collections = ReadJson(key);
  foreach (col in collections) {
    access = col.Collaborators.FirstOrDefault(c => c.UserId == callerId);
    if (access == null) continue;
    books = LoadLibrary(ownerUserId, projectId)
      .Where(b => b.CollectionId == col.Id);
    results.Add(Map(col, books, access, isShared: true));
  }
}
return OrderByName(results);
```

## Связанные артефакты

* gRPC: `#grpc-ListSharedReaderCollections`

---

# Алгоритм cascade clear collection_id на книгах

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-COLL-03

## Логика работы (Псевдокод)

```csharp
// After removing collection from collections index:
books = LoadLibrary(userId, projectId);
books.Where(b => b.CollectionId == collectionId)
     .ForEach(b => { b.CollectionId = null; b.CollectionName = null; });
SaveLibrary(userId, projectId, books);
```

## Связанные артефакты

* gRPC: `#grpc-DeleteReaderCollection`
