# Введение

**Aggregator Service** — единственный trusted inbound caller Media Service в production topology. Aggregator валидирует JWT на REST perimeter, проксирует запросы в gRPC и инжектирует identity context.

Media Service **не** валидирует JWT — доверяет `user_id` header от Aggregator ([[../Методы API/gRPC/04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-02|SR-MEDIA-OPS-02]]).

**SR:** SR-MEDIA-OPS-02, все library RPC (SR-MEDIA-BOOK-*, SR-MEDIA-COLL-*). **КАР:** [[../../02 - Архитектура/03 - КАР-3 - gRPC-only API и контекст user_id|КАР-3]].

# Общая информация

| Параметр | Описание |
| :--- | :--- |
| **Направление** | Inbound (Aggregator → Media Service) |
| **Протокол** | gRPC over HTTP/2 (h2c в Docker network) |
| **Target host** | `media-service:5121` (Compose service name) |
| **Proto package** | `media.MediaService` |
| **Публичный REST mapping** | `Docs/Aggregator Service/04/…/REST API/09 - Медиа и Reader Library (Media).md` |

# Доступ и аутентификация

| Параметр | Описание |
| :--- | :--- |
| **Метод аутентификации** | Network isolation (Docker internal) + trusted BFF |
| **Identity propagation** | gRPC metadata header `user_id` — UUID string из JWT `sub` / user claim |
| **Library RPC** | Обязательный valid `user_id` → иначе `UNAUTHENTICATED` |
| **Upload / Get*Url RPC** | `user_id` **не** проверяется в Media Service; JWT на Aggregator REST routes |

# Ключевые gRPC вызовы (caller perspective)

| Aggregator REST (пример) | Media gRPC | SR |
| :--- | :--- | :--- |
| `POST /api/media/upload-image` | `UploadImage` | SR-MEDIA-STORAGE-01 |
| `POST /api/media/upload-audio` | `UploadAudio` | SR-MEDIA-STORAGE-02 |
| `POST /api/media/upload-document` | `UploadDocument` | SR-MEDIA-STORAGE-03 |
| `GET /api/media/image/{id}` (proxy) | `GetImageUrl` | SR-MEDIA-STORAGE-04 |
| Reader library routes | `ListReaderLibraryBooks`, `SaveReaderLibraryBook`, … | SR-MEDIA-BOOK-*, SR-MEDIA-COLL-* |

# Логика обработки запросов

1. Aggregator auth middleware валидирует JWT.
2. `GrpcContextHelper` (или аналог) добавляет `user_id` в outbound gRPC metadata.
3. Media `MediaGrpcService.GetRequiredUserId` парсит header для library RPC.
4. Upload routes могут вызывать Media без `user_id` — identity enforcement на BFF (rate limit, auth на REST).

# Обработка ошибок

| gRPC status от Media | Aggregator reaction (типично) |
| :--- | :--- |
| `INVALID_ARGUMENT` | HTTP 400 с message |
| `UNAUTHENTICATED` | HTTP 401 |
| `UNAVAILABLE` | HTTP 502/503 |
| `INTERNAL` | HTTP 500 |

## Связанные артефакты

* gRPC: [[../Методы API/gRPC/00 - gRPC - Общая информация]]
* Алгоритм: [[../Алгоритмы и методы бекенда/04 - Платформенные контракты (Operations)#Алгоритм извлечения user_id из gRPC metadata]]
