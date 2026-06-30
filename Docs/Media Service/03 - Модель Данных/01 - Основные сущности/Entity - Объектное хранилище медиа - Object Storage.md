# Введение

Группа **Объектное хранилище медиа** описывает бинарные объекты в bucket S3-совместимого хранилища (MinIO в dev/prod Docker). Каждый объект — **Media Object**: неизменяемый blob с UUID-ключом и префиксом по типу контента.

Media Service не владеет метаданными карточек или терминов — только **файлы** для Card Editor, TTS upload и документов Reader.

---

# Media Object — изображение (`images/{media_id}`)

## 1. Общее описание

**Media Object (Image)** — бинарный файл изображения, сохранённый в bucket с ключом `images/{guid}`. `media_id` генерируется сервисом (`Guid.NewGuid()`) при upload.

Используется для полей карточек (image note fields), превью в UI и inline-отображения через Aggregator proxy.

## 2. Атрибуты (логические поля)

| Название | Тип | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `media_id` | `uuid` | PK (ключ S3) | Идентификатор объекта; часть пути `images/{media_id}`. |
| `content_type` | `string` | NOT NULL, `image/*` | MIME при upload; default `image/png` если пусто. |
| `size_bytes` | `int` | ≤ 5 MB | Размер payload; валидация до PutObject. |
| `bucket` | `string` | NOT NULL | Имя bucket (`Storage:Bucket`, default `polyraspad-media`). |
| `s3_key` | `string` | NOT NULL | Полный ключ: `images/{media_id}`. |

## 3. Связи

| Связанная сущность / сервис | Описание |
| :--- | :--- |
| **Reader Library Book** | Опционально `document_id` указывает на `documents/`, не на images. |
| **AggregatorService** | REST `upload-image` → gRPC `UploadImage`; URL в ответе JSON. |
| **VocabularyService** | Card note fields хранят public URL как строку. |

## 4. Жизненный цикл

1. **Upload:** `PutObject` с `content_type`; возврат `url` + `image_id`.
2. **Read URL:** `GetImageUrl` — server-fetch URL (presigned или `PublicBaseUrl` / `ServerFetchBaseUrl`).
3. **Delete:** явный delete в текущей реализации **не реализован** — объекты персистентны до ручной очистки bucket.

---

# Media Object — аудио (`audio/{media_id}`)

## 1. Общее описание

Бинарный аудиофайл (TTS output upload, audio note fields). Ключ: `audio/{guid}`.

## 2. Атрибуты

| Название | Тип | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `media_id` | `uuid` | PK | Идентификатор; путь `audio/{media_id}`. |
| `content_type` | `string` | `audio/*` | Default `audio/mpeg` если пусто. |
| `s3_key` | `string` | NOT NULL | `audio/{media_id}`. |

## 3. Жизненный цикл

Аналогично image: upload → optional `GetAudioUrl`. TTS синтез на Aggregator может вызвать `UploadAudio` после локальной генерации.

---

# Media Object — документ (`documents/{media_id}`)

## 1. Общее описание

Документ Reader Library или Card Editor: PDF, EPUB или plain text. Ключ: `documents/{guid}`.

Нормализация MIME: `application/pdf`, `application/epub+zip`, `text/plain` — по content-type или расширению `file_name`.

## 2. Атрибуты

| Название | Тип | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `media_id` | `uuid` | PK | `documents/{media_id}`. |
| `content_type` | `string` | PDF/EPUB/TXT | Нормализованный MIME после валидации. |
| `file_name` | `string` | optional | Подсказка для нормализации типа (`.pdf`, `.epub`, `.txt`). |
| `size_bytes` | `int` | ≤ 50 MB | Валидация до upload. |
| `s3_key` | `string` | NOT NULL | `documents/{media_id}`. |

## 3. Связи

| Связанная сущность | Описание |
| :--- | :--- |
| **Reader Library Book** | `document_id` → этот Media Object; URL резолвится при list/save. |
| **AggregatorService** | `extract-document-text` читает файл **на BFF**, не в Media Service. |

## 4. Жизненный цикл

Upload → ссылка в book record → чтение через `GetDocumentUrl` или public URL в ответе upload.
