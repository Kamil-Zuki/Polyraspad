# Введение

Media Service использует **S3-совместимое object storage** (MinIO в Docker dev/prod) как единственный персистентный слой: бинарные медиа и JSON-индексы Reader Library.

**SR:** SR-MEDIA-STORAGE-01…06, SR-MEDIA-BOOK-*, SR-MEDIA-COLL-*. **КАР:** [[../../02 - Архитектура/01 - КАР-1 - S3-совместимое хранилище как единый персистентный слой|КАР-1]].

# Общая информация

| Параметр | Описание |
| :--- | :--- |
| **Версия API** | S3-compatible (AWS SDK for .NET `IAmazonS3`) |
| **Название сервиса** | MinIO (dev/prod Docker) / любой S3-compatible endpoint |
| **Владелец** | Platform / infra |
| **Bucket default** | `polyraspad-media` |
| **Реализация** | `MediaService/Services/S3MediaStorageService.cs` |

# Доступ и аутентификация

| Параметр | Описание |
| :--- | :--- |
| **Метод аутентификации** | AWS Signature V4 (AccessKey + SecretKey) |
| **Хранение учётных данных** | `Storage:AccessKey` / `Storage:SecretKey`; env `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD` в Docker — **не** на frontend |
| **Endpoint** | `Storage:Endpoint` → `http://minio:9000` (Docker network) |
| **Path-style** | `UsePathStyle = true` (обязательно для MinIO) |
| **Среды** | Dev: Compose MinIO `:9000`; Prod: nginx public path + internal fetch URL |

# Ключевые методы S3 API

| Операция SDK | SR | Использование в сервисе |
| :--- | :--- | :--- |
| `PutObject` | SR-MEDIA-STORAGE-01…03, SR-MEDIA-BOOK-02, SR-MEDIA-COLL-02 | Upload blobs и JSON indexes |
| `GetObject` | SR-MEDIA-BOOK-01, SR-MEDIA-COLL-01 | Read `reader-library/…/index.json`, `reader-collections/…/index.json` |
| `GetPreSignedURL` | SR-MEDIA-STORAGE-04…06 | Fallback URL когда base URL не задан |
| `PutBucket` | SR-MEDIA-STORAGE-01 | Lazy bucket create (`EnsureBucketExistsAsync`) |
| `ListObjectsV2` | SR-MEDIA-COLL-06 | Scan prefix `reader-collections/` для shared inbox |

# Key prefixes (S3 object layout)

| Prefix / key pattern | Содержимое |
| :--- | :--- |
| `images/{guid}` | Image blobs (max 5 MB) |
| `audio/{guid}` | Audio blobs |
| `documents/{guid}` | PDF / EPUB / TXT (max 50 MB) |
| `reader-library/{userId}/{projectId}/index.json` | Array `ReaderLibraryBookRecord` |
| `reader-collections/{userId}/{projectId}/index.json` | Array `ReaderCollectionRecord` |

`projectId` в path: trim + replace `/` и `\` на `_`.

# Логика обработки запросов

| Политика | Описание |
| :--- | :--- |
| **Bucket ensure** | Однократный `PutBucket` при первом write; игнор `BucketAlreadyOwnedByYou` |
| **Missing JSON key** | `NoSuchKey` → пустой список (новый user/project) |
| **URL dual model** | `PublicBaseUrl` для browser; `ServerFetchBaseUrl` для BFF — [[../../02 - Архитектура/04 - КАР-4 - Двойная модель URL (public vs server-fetch)|КАР-4]] |
| **Presigned TTL** | `PresignedUrlExpirationMinutes` (default 60) |
| **Retry / circuit breaker** | Не реализованы в Media Service |

# Обработка ошибок

| Тип ошибки | Причина | Реакция сервиса |
| :--- | :--- | :--- |
| `AmazonS3Exception` на upload | MinIO недоступен, quota | gRPC `UNAVAILABLE` |
| `NoSuchKey` на read JSON | Индекс ещё не создан | Пустой список books/collections |
| `InvalidOperationException` | Collection not found (share/unshare) | gRPC `INTERNAL` (unhandled) |

## Связанные артефакты

* Алгоритмы: [[../Алгоритмы и методы бекенда/01 - Загрузка и выдача медиа (Media Storage)#Алгоритм S3 object key layout]]
* Entity: [[../../03 - Модель Данных/01 - Основные сущности/Entity - Объектное хранилище медиа - Object Storage]]
