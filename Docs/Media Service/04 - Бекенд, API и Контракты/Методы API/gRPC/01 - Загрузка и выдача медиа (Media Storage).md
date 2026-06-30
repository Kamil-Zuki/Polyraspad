# Введение

Методы группы **Загрузка и выдача медиа (Media Storage)** обеспечивают сохранение бинарных объектов в S3-совместимое хранилище и резолв URL по UUID. Caller — **Aggregator Service** (и косвенно **VocabularyService** через Aggregator).

Upload RPC **не** требуют `user_id` metadata; identity контролируется на BFF. Get*Url RPC также не требуют `user_id` — доступ к blob на периметре Aggregator (JWT на proxy routes).

**SR:** SR-MEDIA-STORAGE-01 … SR-MEDIA-STORAGE-06. **КАР:** [[02 - Архитектура/01 - КАР-1 - S3-совместимое хранилище как единый персистентный слой|КАР-1]], [[02 - Архитектура/04 - КАР-4 - Двойная модель URL (public vs server-fetch)|КАР-4]].

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-MEDIA-STORAGE-01 | `UploadImage` | Unary | Upload image blob; max 5 MB. |
| SR-MEDIA-STORAGE-02 | `UploadAudio` | Unary | Upload audio blob (TTS persist). |
| SR-MEDIA-STORAGE-03 | `UploadDocument` | Unary | Upload PDF/EPUB/TXT; max 50 MB. |
| SR-MEDIA-STORAGE-04 | `GetImageUrl` | Unary | Resolve URL для `image_id`. |
| SR-MEDIA-STORAGE-05 | `GetAudioUrl` | Unary | Resolve URL для `audio_id`. |
| SR-MEDIA-STORAGE-06 | `GetDocumentUrl` | Unary | Resolve URL для `document_id`. |

---

<span id="grpc-UploadImage"></span>

# SR-MEDIA-STORAGE-01: Upload image: UploadImage

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Загрузка и выдача медиа (Media Storage)#SR-MEDIA-STORAGE-01]]

| Сигнатура | `rpc UploadImage(UploadImageRequest) returns (UploadImageResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `UploadImageRequest` — `image_data` (bytes), `content_type` (string) |
| **Сообщение ответа** | `UploadImageResponse` — `url`, `image_id` (UUID string) |

## Логика обработки запроса

1. Проверить, что `image_data` не пуст (иначе `INVALID_ARGUMENT`).
2. Проверить размер ≤ 5 MB (иначе `INVALID_ARGUMENT`).
3. Нормализовать `content_type`: default `image/png`; должен начинаться с `image/`.
4. `EnsureBucketExists` — lazy create bucket при первом upload.
5. Генерировать `Guid`, `PutObject` в ключ `images/{guid}` с MIME.
6. Построить public/presigned URL через `GetMediaUrlAsync`.
7. Вернуть `url` и `image_id`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Пустой payload, размер > 5 MB, MIME не `image/*`. |
| **UNAVAILABLE** | S3/MinIO ошибка при PutObject. |

---

<span id="grpc-UploadAudio"></span>

# SR-MEDIA-STORAGE-02: Upload audio: UploadAudio

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Загрузка и выдача медиа (Media Storage)#SR-MEDIA-STORAGE-02]]

| Сигнатура | `rpc UploadAudio(UploadAudioRequest) returns (UploadAudioResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `UploadAudioRequest` — `audio_data`, `content_type` |
| **Сообщение ответа** | `UploadAudioResponse` — `url`, `audio_id` |

## Логика обработки запроса

1. Проверить, что `audio_data` не пуст.
2. Нормализовать `content_type`: default `audio/mpeg`; должен начинаться с `audio/`.
3. `PutObject` в `audio/{guid}`.
4. Построить URL, вернуть `url` и `audio_id`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Пустой payload, MIME не `audio/*`. |
| **UNAVAILABLE** | S3 ошибка PutObject. |

---

<span id="grpc-UploadDocument"></span>

# SR-MEDIA-STORAGE-03: Upload document: UploadDocument

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Загрузка и выдача медиа (Media Storage)#SR-MEDIA-STORAGE-03]]

| Сигнатура | `rpc UploadDocument(UploadDocumentRequest) returns (UploadDocumentResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `UploadDocumentRequest` — `document_data`, `content_type`, `file_name` |
| **Сообщение ответа** | `UploadDocumentResponse` — `url`, `document_id` |

## Логика обработки запроса

1. Проверить непустой `document_data` и размер ≤ 50 MB.
2. Нормализовать MIME: PDF (`application/pdf`), EPUB (`application/epub+zip`), plain text (`text/plain`) — по `content_type` или расширению `file_name`.
3. При неподдерживаемом типе — `INVALID_ARGUMENT`.
4. `PutObject` в `documents/{guid}`.
5. Вернуть `url` и `document_id`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Пустой payload, > 50 MB, unsupported document type. |
| **UNAVAILABLE** | S3 ошибка PutObject. |

---

<span id="grpc-GetImageUrl"></span>

# SR-MEDIA-STORAGE-04: Get image URL: GetImageUrl

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Загрузка и выдача медиа (Media Storage)#SR-MEDIA-STORAGE-04]]

| Сигнатура | `rpc GetImageUrl(GetImageUrlRequest) returns (GetImageUrlResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `GetImageUrlRequest` — `image_id` (UUID string) |
| **Сообщение ответа** | `GetImageUrlResponse` — `url` |

## Логика обработки запроса

1. Валидировать `image_id` как UUID (иначе `INVALID_ARGUMENT`).
2. Построить server-fetch URL для `images/{image_id}`: приоритет `ServerFetchBaseUrl` > `PublicBaseUrl` > presigned ([[02 - Архитектура/04 - КАР-4 - Двойная модель URL (public vs server-fetch)|КАР-4]]).
3. Вернуть `url`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing/invalid `image_id`. |

---

<span id="grpc-GetAudioUrl"></span>

# SR-MEDIA-STORAGE-05: Get audio URL: GetAudioUrl

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Загрузка и выдача медиа (Media Storage)#SR-MEDIA-STORAGE-05]]

| Сигнатура | `rpc GetAudioUrl(GetAudioUrlRequest) returns (GetAudioUrlResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `GetAudioUrlRequest` — `audio_id` |
| **Сообщение ответа** | `GetAudioUrlResponse` — `url` |

## Логика обработки запроса

1. Валидировать `audio_id` как UUID.
2. Построить server-fetch URL для `audio/{audio_id}` (тот же приоритет баз URL).
3. Вернуть `url`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing/invalid `audio_id`. |

---

<span id="grpc-GetDocumentUrl"></span>

# SR-MEDIA-STORAGE-06: Get document URL: GetDocumentUrl

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Загрузка и выдача медиа (Media Storage)#SR-MEDIA-STORAGE-06]]

| Сигнатура | `rpc GetDocumentUrl(GetDocumentUrlRequest) returns (GetDocumentUrlResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `GetDocumentUrlRequest` — `document_id` |
| **Сообщение ответа** | `GetDocumentUrlResponse` — `url` |

## Логика обработки запроса

1. Валидировать `document_id` как UUID.
2. Построить server-fetch URL для `documents/{document_id}`.
3. Вернуть `url`. Auth не проверяется в Media — JWT на Aggregator proxy (`serve-document`).

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Missing/invalid `document_id`. |
