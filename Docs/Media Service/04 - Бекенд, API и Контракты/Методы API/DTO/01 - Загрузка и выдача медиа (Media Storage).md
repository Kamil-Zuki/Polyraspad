# Введение

DTO группы **Media Storage** — request/response messages для upload и URL resolution. Поля соответствуют [[03 - Модель Данных/01 - Основные сущности/Entity - Объектное хранилище медиа - Object Storage|Media Object]].

# 1. Список DTO

| DTO | Тип | gRPC |
| :--- | :--- | :--- |
| `UploadImageRequest` / `UploadImageResponse` | Запрос / Ответ | `#grpc-UploadImage` |
| `UploadAudioRequest` / `UploadAudioResponse` | Запрос / Ответ | `#grpc-UploadAudio` |
| `UploadDocumentRequest` / `UploadDocumentResponse` | Запрос / Ответ | `#grpc-UploadDocument` |
| `GetImageUrlRequest` / `GetImageUrlResponse` | Запрос / Ответ | `#grpc-GetImageUrl` |
| `GetAudioUrlRequest` / `GetAudioUrlResponse` | Запрос / Ответ | `#grpc-GetAudioUrl` |
| `GetDocumentUrlRequest` / `GetDocumentUrlResponse` | Запрос / Ответ | `#grpc-GetDocumentUrl` |

---

<span id="dto-UploadImageRequest"></span>

# DTO: UploadImageRequest

## Контекст и назначение

gRPC upload image. Caller: Aggregator `POST upload-image`.

**Назначение:** Запрос
**Реализация сущности:** Media Object (image) — proto field `image_data`

## Структура данных

| Имя поля (JSON на BFF) | Тип | Описание |
| :--- | :--- | :--- |
| `image_data` | `bytes` | Бинарный payload (≤ 5 MB) |
| `content_type` | `string` | MIME `image/*`; default `image/png` |

---

<span id="dto-UploadImageResponse"></span>

# DTO: UploadImageResponse

## Контекст и назначение

**Назначение:** Ответ
**Реализация сущности:** `media_id` + resolved URL

## Структура данных

| Имя поля | Тип | Описание |
| :--- | :--- | :--- |
| `url` | `string` | Public или presigned URL |
| `image_id` | `string` (UUID) | Идентификатор `images/{id}` |

## Пример работы (JSON)

```json
{
  "url": "https://api.example.com/polyraspad-media/images/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "image_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

<span id="dto-UploadDocumentRequest"></span>

# DTO: UploadDocumentRequest

## Контекст и назначение

Reader import, document upload. **SR-MEDIA-STORAGE-03**.

## Структура данных

| Имя поля | Тип | Описание |
| :--- | :--- | :--- |
| `document_data` | `bytes` | PDF/EPUB/TXT (≤ 50 MB) |
| `content_type` | `string` | MIME hint |
| `file_name` | `string` | Расширение для нормализации типа |

---

<span id="dto-GetDocumentUrlResponse"></span>

# DTO: GetDocumentUrlResponse

## Структура данных

| Имя поля | Тип | Описание |
| :--- | :--- | :--- |
| `url` | `string` | Server-fetch URL для BFF proxy |
