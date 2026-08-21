# Введение

Настоящий документ содержит полное описание **gRPC** интерфейса микросервиса **Media Service**. Это **единственный публичный машинный контракт** сервиса: REST и WebSocket **не** экспонируются. Клиенты (SPA, extension) обращаются к **Aggregator Service**, который валидирует JWT, проксирует gRPC и передаёт `user_id` в metadata.

Media Service владеет бинарными объектами в S3-совместимом bucket (MinIO в dev) и JSON-индексами Reader Library. В соответствии с [[02 - Архитектура/03 - КАР-3 - gRPC-only API и контекст user_id|КАР-3]], сервис **не** валидирует JWT — доверяет `user_id` от trusted caller (Aggregator) и изолирует library data по owner paths в object storage.

**Публичный REST mapping:** `Docs/Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/09 - Медиа и Reader Library (Media).md`.

## Service

| Поле | Значение |
| :--- | :--- |
| Package | `media` |
| Service | `MediaService` |
| C# namespace | `Pvs.Media.Grpc` |
| Port | `5121` (HTTP/2 / h2c) |
| Proto | `MediaService/Protos/media.proto` (копия — [[media.proto]]) |
| Max message size | 1000 MB send/receive |

## Metadata (inbound)

| Key | Источник | Описание |
| :--- | :--- | :--- |
| `user_id` | Aggregator (из JWT) | Guid string; **обязателен** для library RPC ([[04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-02\|SR-MEDIA-OPS-02]]) |

Upload/Get*Url RPC **не** проверяют `user_id` в текущей реализации.

# 1. Группы методов gRPC

| Группа | Описание | Файл |
| :--- | :--- | :--- |
| **Загрузка и выдача медиа (Media Storage)** | Upload image/audio/document и резолв URL по UUID. | [[01 - Загрузка и выдача медиа (Media Storage)]] |
| **Reader Library — книги (Reader Books)** | List, save, delete метаданных книг в project library. | [[02 - Reader Library — книги (Reader Books)]] |
| **Reader Library — коллекции и шаринг (Reader Collections)** | CRUD коллекций, share/unshare, inbox shared collections. | [[03 - Reader Library — коллекции и шаринг (Reader Collections)]] |
| **Платформенные контракты (Operations)** | Нет RPC в proto; health-check HTTP и правила `user_id` metadata. | [[04 - Платформенные контракты (Operations)]] |

# 2. Загрузка и выдача медиа (Media Storage)

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-MEDIA-STORAGE-01 | `UploadImage` | Unary | PutObject в `images/{guid}`; max 5 MB, `image/*`. |
| SR-MEDIA-STORAGE-02 | `UploadAudio` | Unary | PutObject в `audio/{guid}`; `audio/*`. |
| SR-MEDIA-STORAGE-03 | `UploadDocument` | Unary | PDF/EPUB/TXT в `documents/{guid}`; max 50 MB. |
| SR-MEDIA-STORAGE-04 | `GetImageUrl` | Unary | Server-fetch или public URL для `image_id`. |
| SR-MEDIA-STORAGE-05 | `GetAudioUrl` | Unary | URL для `audio_id`. |
| SR-MEDIA-STORAGE-06 | `GetDocumentUrl` | Unary | URL для `document_id` (Reader, BFF proxy). |

# 3. Reader Library — книги (Reader Books)

Требуют metadata `user_id` ([[04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-02|SR-MEDIA-OPS-02]]).

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-MEDIA-BOOK-01 | `ListReaderLibraryBooks` | Unary | Список книг owner в `project_id` с resolved document URL. |
| SR-MEDIA-BOOK-02 | `SaveReaderLibraryBook` | Unary | Upsert метаданных книги (title, document link, progress). |
| SR-MEDIA-BOOK-03 | `DeleteReaderLibraryBook` | Unary | Удаление записи из JSON-индекса (blob не удаляется). |

# 4. Reader Library — коллекции и шаринг (Reader Collections)

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-MEDIA-COLL-01 | `ListReaderCollections` | Unary | Коллекции owner + nested books. |
| SR-MEDIA-COLL-02 | `SaveReaderCollection` | Unary | Upsert коллекции и collaborators snapshot. |
| SR-MEDIA-COLL-03 | `DeleteReaderCollection` | Unary | Удаление коллекции; книги теряют `collection_id`. |
| SR-MEDIA-COLL-04 | `ShareReaderCollection` | Unary | Добавление collaborator. |
| SR-MEDIA-COLL-05 | `UnshareReaderCollection` | Unary | Удаление collaborator. |
| SR-MEDIA-COLL-06 | `ListSharedReaderCollections` | Unary | Inbox коллекций, расшаренных с caller. |

# 5. Платформенные контракты (Operations)

| Код требования | Контракт | Описание |
| :--- | :--- | :--- |
| SR-MEDIA-OPS-01 | `GET /healthz` (HTTP) | Liveness на порту 5121; не gRPC. |
| SR-MEDIA-OPS-02 | gRPC metadata `user_id` | Обязательный header для library RPC. |

Итого в `media.proto`: **15** unary RPC. Детали — в групповых файлах и [[04 - Платформенные контракты (Operations)]].
