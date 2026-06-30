# Введение

Настоящий индекс описывает **персистентные представления данных** микросервиса **Media Service** — источник истины для объектного хранилища медиа и метаданных Reader Library.

В отличие от доменных сервисов с PostgreSQL, Media Service **не использует реляционную БД**. Все данные живут в **S3-совместимом bucket** (`polyraspad-media` по умолчанию): бинарные объекты по префиксам и JSON-индексы для библиотеки читателя.

## Группы сущностей

| Группа | Файл | Ключи / формат |
| :--- | :--- | :--- |
| Объектное хранилище медиа | [[Entity - Объектное хранилище медиа - Object Storage]] | `images/{uuid}`, `audio/{uuid}`, `documents/{uuid}` |
| Reader Library | [[Entity - Reader Library - Reader Library]] | `reader-library/{userId}/{projectId}/index.json`, `reader-collections/{userId}/{projectId}/index.json` |

## gRPC ↔ сущности

| RPC | Основные сущности |
| :--- | :--- |
| `UploadImage`, `GetImageUrl` | Media Object (`images/`) |
| `UploadAudio`, `GetAudioUrl` | Media Object (`audio/`) |
| `UploadDocument`, `GetDocumentUrl` | Media Object (`documents/`) |
| `ListReaderLibraryBooks`, `SaveReaderLibraryBook`, `DeleteReaderLibraryBook` | Reader Library Book (в JSON-индексе) |
| `ListReaderCollections`, `SaveReaderCollection`, `DeleteReaderCollection` | Reader Collection (+ Collaborators) |
| `ShareReaderCollection`, `UnshareReaderCollection` | Reader Collection Collaborator |
| `ListSharedReaderCollections` | Reader Collection (cross-user scan) |

## Контекст вызова

Все library RPC и upload ожидают gRPC metadata **`user_id`** (UUID) — прокидывается Aggregator из JWT. Media Service **не** валидирует JWT сам.

## Лимиты (валидация в gRPC)

| Тип | Max size | Content-type |
| :--- | :--- | :--- |
| Image | 5 MB | `image/*` |
| Document | 50 MB | PDF, EPUB, plain text |
| Audio | без жёсткого лимита в коде | `audio/*` |
