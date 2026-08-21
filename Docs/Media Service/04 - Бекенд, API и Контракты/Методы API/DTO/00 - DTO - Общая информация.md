# Введение

Настоящий документ описывает структуры данных (DTO / proto messages) микросервиса **Media Service**. На периметре Aggregator JSON поля маппятся на proto messages 1:1; Media Service сериализует JSON-индексы Reader Library в object storage с теми же полями.

Публичные REST JSON shapes — на **Aggregator Service** (`09 - Медиа и Reader Library`); здесь — **source of truth** для gRPC и S3 JSON records.

Документ основан на SR-MEDIA-* и сущностях [[03 - Модель Данных/01 - Основные сущности/Entity - Объектное хранилище медиа - Object Storage|Object Storage]], [[03 - Модель Данных/01 - Основные сущности/Entity - Reader Library - Reader Library|Reader Library]].

# 1. Группы DTO

| Группа | Файл | SR |
| :--- | :--- | :--- |
| Загрузка и выдача медиа (Media Storage) | [[01 - Загрузка и выдача медиа (Media Storage)]] | SR-MEDIA-STORAGE-01 … 06 |
| Reader Library — книги (Reader Books) | [[02 - Reader Library — книги (Reader Books)]] | SR-MEDIA-BOOK-01 … 03 |
| Reader Library — коллекции и шаринг (Reader Collections) | [[03 - Reader Library — коллекции и шаринг (Reader Collections)]] | SR-MEDIA-COLL-01 … 06 |
| Платформенные контракты (Operations) | [[04 - Платформенные контракты (Operations)]] | SR-MEDIA-OPS-02 (metadata) |

# 2. Загрузка и выдача медиа (Media Storage)

| Название | Назначение | gRPC |
| :--- | :--- | :--- |
| `UploadImageRequest` / `UploadImageResponse` | Upload image | `#grpc-UploadImage` |
| `UploadAudioRequest` / `UploadAudioResponse` | Upload audio | `#grpc-UploadAudio` |
| `UploadDocumentRequest` / `UploadDocumentResponse` | Upload document | `#grpc-UploadDocument` |
| `GetImageUrlRequest` / `GetImageUrlResponse` | Resolve image URL | `#grpc-GetImageUrl` |
| `GetAudioUrlRequest` / `GetAudioUrlResponse` | Resolve audio URL | `#grpc-GetAudioUrl` |
| `GetDocumentUrlRequest` / `GetDocumentUrlResponse` | Resolve document URL | `#grpc-GetDocumentUrl` |

# 3. Reader Library — книги (Reader Books)

| Название | Назначение | gRPC |
| :--- | :--- | :--- |
| `ReaderLibraryBook` | Книга в индексе и gRPC response | Book RPCs |
| `ListReaderLibraryBooksRequest` / `Response` | List books | `#grpc-ListReaderLibraryBooks` |
| `SaveReaderLibraryBookRequest` / `Response` | Upsert book | `#grpc-SaveReaderLibraryBook` |
| `DeleteReaderLibraryBookRequest` / `Response` | Delete book | `#grpc-DeleteReaderLibraryBook` |

# 4. Reader Library — коллекции и шаринг (Reader Collections)

| Название | Назначение | gRPC |
| :--- | :--- | :--- |
| `ReaderCollection` | Коллекция + nested books | Collection RPCs |
| `ReaderCollectionCollaborator` | Collaborator snapshot | Share RPCs |
| `ListReaderCollectionsRequest` / `Response` | List collections | `#grpc-ListReaderCollections` |
| `SaveReaderCollectionRequest` / `Response` | Upsert collection | `#grpc-SaveReaderCollection` |
| `DeleteReaderCollectionRequest` / `Response` | Delete collection | `#grpc-DeleteReaderCollection` |
| `ShareReaderCollectionRequest` / `Response` | Share | `#grpc-ShareReaderCollection` |
| `UnshareReaderCollectionRequest` / `Response` | Unshare | `#grpc-UnshareReaderCollection` |
| `ListSharedReaderCollectionsRequest` / `Response` | Shared inbox | `#grpc-ListSharedReaderCollections` |
