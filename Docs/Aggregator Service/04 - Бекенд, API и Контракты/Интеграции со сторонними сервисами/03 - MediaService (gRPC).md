# Введение

Файлы, TTS, document extract, Reader Library. Proto: `media.proto`. Config: `AggregatorService:MediaServiceBaseUrl`.

# Общая информация

| Параметр | Значение |
| :--- | :--- |
| **SR** | SR-AGG-MEDIA-* |
| **Storage** | MinIO S3-compatible (MediaService proxy) |

# Используемые gRPC методы

| REST | gRPC |
| :--- | :--- |
| POST /api/Media/upload | UploadImage / UploadDocument |
| POST /api/Media/generate-audio | GenerateAudio (или BFF TTS — см. algorithms) |
| POST /api/Media/extract-text | ExtractDocumentText |
| GET /api/Media/serve-* | GetPublicUrl / stream proxy |
| Reader Library routes | SaveBook, ListBooks, Collections CRUD |

# BFF-side дополнения

Часть extract/TTS выполняется на Aggregator (PDF parsing, espeak) — см. [[02 - AI Proxy, TTS и извлечение текста]].

# Public URLs

`MINIO_PUBLIC_BASE_URL` — формирование client-facing media links в JSON responses.
