# Введение

Алгоритмы группы **Media Storage**: сохранение blobs в S3 и построение URL для клиента и server-fetch.

# 1. Список алгоритмов

| Алгоритм | SR |
| :--- | :--- |
| S3 object key layout | SR-MEDIA-STORAGE-01 … 03, SR-MEDIA-BOOK-*, SR-MEDIA-COLL-* |
| Загрузка медиа в S3 (upload path) | SR-MEDIA-STORAGE-01 … 03 |
| Резолв URL (public vs server-fetch) | SR-MEDIA-STORAGE-04 … 06 |

---

# Алгоритм S3 object key layout

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-STORAGE-01…03, SR-MEDIA-BOOK-*, SR-MEDIA-COLL-*

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Blob keys: `images/`, `audio/`, `documents/` + UUID |
| 2 | JSON indices: `reader-library/`, `reader-collections/` per owner + project |

## Логика работы (Псевдокод)

```csharp
// Blobs
$"{prefix}/{mediaId}"  // prefix ∈ { images, audio, documents }

// JSON indices
$"reader-library/{userId:D}/{Sanitize(projectId)}/index.json"
$"reader-collections/{userId:D}/{Sanitize(projectId)}/index.json"
```

## Связанные артефакты

* КАР-1, КАР-2
* Интеграция: [[../Интеграции со сторонними сервисами/01 - MinIO (S3)#Key prefixes (S3 object layout)]]

---

# Алгоритм загрузки медиа в S3 (upload path)

## Контекст и область применения

### Почему был создан

Единый путь persist бинарных медиа без PostgreSQL — все blobs в object storage с UUID keys.

### Бизнес-требование

SR-MEDIA-STORAGE-01, SR-MEDIA-STORAGE-02, SR-MEDIA-STORAGE-03

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Card Editor image upload |
| 2 | TTS audio persist после синтеза на Aggregator |
| 3 | Reader document import (PDF/EPUB/TXT) |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Image max 5 MB; document max 50 MB |
| 2 | Нет delete RPC — blobs персистентны |

## Входные данные

| Параметр | Тип | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `data` | `bytes` | Blob payload | Да |
| `content_type` | `string` | MIME | Да |
| `prefix` | `string` | `images` / `audio` / `documents` | Да (implicit по RPC) |
| `file_name` | `string` | Hint для document MIME | Нет |

## Выходные данные

| Параметр | Тип | Описание |
| :--- | :--- | :--- |
| `media_id` | `uuid` | Новый Guid |
| `url` | `string` | Public/presigned URL |
| `s3_key` | `string` | `{prefix}/{media_id}` |

## Логика работы (Псевдокод)

```csharp
// 1. EnsureBucketExists (lazy once per process)
// 2. Validate size/MIME на границе gRPC
var id = Guid.NewGuid();
var key = $"{prefix}/{id}";
await s3.PutObjectAsync(bucket, key, stream, contentType);
var url = await ResolvePublicUrl(id, prefix);
return (id, url);
```

## Связанные артефакты

* gRPC: `#grpc-UploadImage`, `#grpc-UploadAudio`, `#grpc-UploadDocument`
* КАР-1: S3 как единый персистентный слой
* Интеграция: [[../Интеграции со сторонними сервисами/01 - MinIO (S3)]]

---

# Алгоритм резолва URL (public vs server-fetch)

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-STORAGE-04, SR-MEDIA-STORAGE-05, SR-MEDIA-STORAGE-06

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Aggregator `serve-image` / `serve-document` / `serve-audio` |
| 2 | Hydration `url` в `ReaderLibraryBook` responses |
| 3 | Inline URL в upload responses |

## Входные данные

| Параметр | Тип | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `media_id` | `uuid` | Object id | Да |
| `prefix` | `string` | S3 prefix | Да |
| `mode` | enum | `Public` vs `ServerFetch` | Да |

## Выходные данные

| Параметр | Тип | Описание |
| :--- | :--- | :--- |
| `url` | `string` | Resolved URL |

## Логика работы (Псевдокод)

```csharp
var key = $"{prefix}/{mediaId}";
// ServerFetch (Get*Url RPC, library hydration):
var baseUrl = options.ServerFetchBaseUrl
    ?? options.PublicBaseUrl
    ?? presigned(bucket, key, ttlMinutes);
return $"{baseUrl.TrimEnd('/')}/{key}";

// Public (upload response):
var baseUrl = options.PublicBaseUrl ?? presigned(...);
```

## Связанные артефакты

* gRPC: `#grpc-GetImageUrl`, `#grpc-GetAudioUrl`, `#grpc-GetDocumentUrl`
* КАР-4: Двойная модель URL
