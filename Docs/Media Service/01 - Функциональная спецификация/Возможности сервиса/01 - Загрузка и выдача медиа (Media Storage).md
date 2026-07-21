# Группа 1: Загрузка и выдача медиа (Media Storage)

## Введение

В этом разделе описывается gRPC-слой Media Service для **загрузки бинарных медиа** в S3-совместимое хранилище и **резолва URL** по идентификатору объекта.

Aggregator валидирует размер/MIME на BFF до gRPC; Media Service повторяет критичные проверки на границе сервиса.

**Метафора:**

Представьте **склад с маркированными контейнерами**. Каждый upload получает UUID-ярлык (`image_id`, `audio_id`, `document_id`); выдача URL — «адрес ячейки» для крана (browser) или внутреннего погрузчика (Aggregator HttpClient).

gRPC контракт: `media.proto` — `UploadImage`, `UploadAudio`, `UploadDocument`, `Get*Url`.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Загрузка и выдача медиа (Media Storage).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-MEDIA-STORAGE-01** | **Upload image:** PutObject в `images/{id}`; max 5 MB, `image/*`. |
| **SR-MEDIA-STORAGE-02** | **Upload audio:** PutObject в `audio/{id}`; `audio/*`. |
| **SR-MEDIA-STORAGE-03** | **Upload document:** PDF/EPUB/TXT в `documents/{id}`; max 50 MB. |
| **SR-MEDIA-STORAGE-04** | **Get image URL:** Public или server-fetch URL по UUID. |
| **SR-MEDIA-STORAGE-05** | **Get audio URL:** URL для audio object. |
| **SR-MEDIA-STORAGE-06** | **Get document URL:** URL для document object (Reader, proxy). |

---

# Детальная спецификация требований

## SR-MEDIA-STORAGE-01: Upload image {#SR-MEDIA-STORAGE-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Size limit** | Payload ≤ 5 MB → иначе `InvalidArgument`. |
| **MIME** | Content-type должен начинаться с `image/`; default `image/png`. |
| **UUID key** | Новый `Guid` на каждый upload. |
| **Response** | `url` + `image_id` для Card Editor / note fields. |

### 2. Высокоуровневое описание

Представим upload image как **регистрацию контейнера на складе с QR-ярлыком**.

1. **Приём груза:** Caller (Aggregator) передаёт `bytes` и `content_type` на границу gRPC `UploadImage`.
2. **Проверка на входе:** Сервис проверяет размер (≤ 5 MB) и MIME (`image/*`, default `image/png`); иначе — `InvalidArgument` без `PutObject`.
3. **Маркировка и размещение:** Для валидного payload генерируется новый `Guid` и выполняется `PutObject` в bucket по ключу `images/{guid}`.
4. **Выдача адреса:** Upload response URL строится через `GetMediaUrlAsync` (`PublicBaseUrl` → else presigned) + `image_id`. Поздний `GetImageUrl` использует **другую** базу (`ServerFetchBaseUrl` cascade) — см. `03` dual URL model.

Таким образом, каждый upload изображения получает стабильный UUID-идентификатор и URL; Aggregator валидирует размер/MIME на BFF, Media Service повторяет критичные проверки на границе сервиса.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Upload для Card Editor (Happy Path)

1. **gRPC:** `UploadImage` с image bytes.
2. **S3:** `PutObject` success.
3. **Ответ:** `url`, `image_id`; BFF → HTTP 201.

#### Сценарий Б: Payload too large (Negative Path)

1. **gRPC:** image > 5 MB.
2. **Ответ:** `InvalidArgument` без PutObject.

---

## SR-MEDIA-STORAGE-02: Upload audio {#SR-MEDIA-STORAGE-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **MIME** | `audio/*`; default `audio/mpeg`. |
| **Use case** | TTS output upload после синтеза на Aggregator. |
| **Key** | `audio/{guid}`. |

### 2. Высокоуровневое описание

Представим persist TTS-аудио как **оприходование записи на аудиополку склада**.

1. **Синтез на BFF:** Aggregator синтезирует audio локально (TTS output) после синтеза на Aggregator.
2. **Передача в Media:** Вызывается gRPC `UploadAudio` с `bytes` и `content_type` (`audio/*`, default `audio/mpeg`).
3. **Сохранение в MinIO:** Сервис пишет объект по ключу `audio/{guid}` через `PutObject`.
4. **Единый путь для study:** В ответ возвращаются `url` и `audio_id` — единый CDN path для study session и card fields.

Таким образом, TTS-результат persist'ится в S3-совместимое хранилище и становится доступен по стабильному идентификатору без дублирования логики хранения на BFF.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: TTS persist (Happy Path)

1. **BFF:** synthesize → `UploadAudio`.
2. **Ответ:** `url`, `audio_id` для card field.

---

## SR-MEDIA-STORAGE-03: Upload document {#SR-MEDIA-STORAGE-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Size** | ≤ 50 MB. |
| **Types** | PDF, EPUB, plain text — нормализация по MIME и `file_name`. |
| **Reader import** | Документ далее ссылается `document_id` в book record. |

### 2. Высокоуровневое описание

Представим upload document как **первый шаг конвейера импорта книги на склад документов**.

1. **Загрузка файла:** Caller передаёт PDF/EPUB/plain text через gRPC `UploadDocument` с `bytes`, MIME и `file_name`.
2. **Проверка границ:** Сервис проверяет размер (≤ 50 MB) и поддерживаемый тип; неподдерживаемый content-type (например, `application/zip`) → `InvalidArgument`.
3. **Размещение blob:** Валидный документ сохраняется в `documents/{guid}`.
4. **Ссылка для Reader:** В ответ возвращаются `document_id` и `url`; book record далее ссылается на `document_id`, а extract текста выполняется на Aggregator (`IDocumentTextExtractor`), не в Media Service.

Таким образом, Media Service отвечает только за хранение бинарного документа; import pipeline продолжается на BFF.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Import EPUB (Happy Path)

1. **gRPC:** `UploadDocument` с epub bytes + `file_name`.
2. **Ответ:** `document_id`, `url`.

#### Сценарий Б: Unsupported type (Negative Path)

1. **gRPC:** content-type `application/zip`.
2. **Ответ:** `InvalidArgument` — unsupported document type.

---

## SR-MEDIA-STORAGE-04: Get image URL {#SR-MEDIA-STORAGE-04}

Резолв server-fetch или public URL для `images/{image_id}`.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **UUID validation** | Invalid/missing `image_id` → `InvalidArgument`. |
| **Server-fetch priority** | `ServerFetchBaseUrl` > `PublicBaseUrl` > presigned. |

### 2. Высокоуровневое описание

Представим Get image URL как **запрос адреса ячейки по QR-ярлыку**.

1. **Запрос по id:** Aggregator вызывает gRPC `GetImageUrl(image_id)` — для BFF proxy (`serve-image`) или inline URL в DTO.
2. **Валидация UUID:** Invalid/missing `image_id` → `InvalidArgument`.
3. **Резолв URL:** Сервис строит URL для `images/{image_id}` с приоритетом `ServerFetchBaseUrl` > `PublicBaseUrl` > presigned.
4. **Использование на BFF:** Возвращённый server-fetch URL применяется для proxy или напрямую в ответе клиенту.

Таким образом, клиент получает актуальный URL изображения без прямого доступа к MinIO и без дублирования логики резолва на BFF.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Resolve image for proxy (Happy Path)

1. **gRPC:** `GetImageUrl(image_id)`.
2. **Ответ:** server-fetch URL для `serve-image`.

---

## SR-MEDIA-STORAGE-05: Get audio URL {#SR-MEDIA-STORAGE-05}

Резолв URL для `audio/{audio_id}` — TTS upload и audio note fields.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **UUID validation** | Valid audio UUID required. |
| **URL bases** | Same priority as image/document. |

### 2. Высокоуровневое описание

Представим Get audio URL как **запрос номера аудиозаписи на полке по её ярлыку**.

1. **Запрос по id:** После TTS upload на Aggregator клиент или BFF вызывает gRPC `GetAudioUrl(audio_id)`.
2. **Валидация UUID:** Valid audio UUID required; invalid/missing id → `InvalidArgument`.
3. **Резолв URL:** Сервис строит URL для `audio/{audio_id}` с тем же приоритетом баз, что у image/document.
4. **Отображение в UI:** Возвращённый URL используется для `<audio src>` через proxy или public path в study session.

Таким образом, TTS upload и audio note fields получают stable URL по `audio_id` без повторной загрузки blob.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Study session audio (Happy Path)

1. **gRPC:** `GetAudioUrl(audio_id)`.
2. **Ответ:** URL для `<audio src>` через proxy или public path.

---

## SR-MEDIA-STORAGE-06: Get document URL {#SR-MEDIA-STORAGE-06}

Резолв URL для `documents/{document_id}` — Reader и `serve-document` proxy.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **UUID validation** | Invalid/missing id → `InvalidArgument`. |
| **No auth in Media** | Доступ контролируется BFF (JWT на proxy). |

### 2. Высокоуровневое описание

Представим Get document URL как **запрос внутреннего адреса документа для крана погрузчика**.

1. **Запрос по id:** Aggregator вызывает gRPC `GetDocumentUrl(document_id)` — когда нужен internal fetch URL для `serve-document` или при маппинге library books.
2. **Валидация UUID:** Invalid/missing `document_id` → `InvalidArgument`.
3. **Резолв URL:** Сервис строит server-fetch или public URL для `documents/{document_id}`.
4. **Контроль доступа на BFF:** Media Service не проверяет auth; доступ контролируется BFF (JWT на proxy), BFF HttpClient stream to client.

Таким образом, Reader и `serve-document` proxy получают internal fetch URL без встраивания JWT-логики в Media Service.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Resolve document for proxy (Happy Path)

1. **gRPC:** `GetDocumentUrl(document_id)`.
2. **Ответ:** server-fetch URL; BFF HttpClient stream to client.

---

*Следующая группа: [[02 - Reader Library — книги (Reader Books)]].*
