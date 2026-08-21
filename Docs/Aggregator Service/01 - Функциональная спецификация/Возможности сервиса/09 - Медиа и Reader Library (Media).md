# Группа 9: Медиа и Reader Library (Media)

## Введение

В этом разделе описывается REST-слой Aggregator Service для **MediaService** (upload, TTS), **локальной обработки на BFF** (extract PDF/EPUB/TXT, HTTP proxy для CORS) и **Reader Library** (books, collections, share).

Upload/TTS идут в gRPC MediaService → MinIO. Extract-document-text и serve-image/serve-document выполняются **на Aggregator** (fetch + local parsers / HttpClient proxy).

**Метафора:**

Представьте **фото- и архивное бюро при читальном зале**. Вы приносите файл — бюро кладёт его в хранилище (MinIO через MediaService) или читает текст на стойке (extract на BFF). Для картинок с другого домена бюро открывает **прокси-окно** (serve-image), чтобы браузер не упёрся в CORS.

REST-контракты: [[04 - Бекенд, API и Контракты/Методы API/REST API/09 - Медиа и Reader Library (Media)|REST API — Media]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к медиа и Reader Library.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-MEDIA-01** | **Загрузка медиа и синтез речи:** Upload image/document в MinIO через MediaService и generate-audio (TTS); лимиты 5 MB / 50 MB. |
| **SR-AGG-MEDIA-02** | **Извлечение текста из документа:** Локальный parse PDF/EPUB/TXT на BFF для import в Reader library. |
| **SR-AGG-MEDIA-03** | **Same-origin прокси медиа:** Credentialed serve-image/serve-document — preview без CORS-блокировки для authenticated users. |
| **SR-AGG-MEDIA-04** | **Библиотека чтения (Reader library):** Книги, коллекции, share/unshare collaborators и список shared collections. |

---

# Детальная спецификация требований

## SR-AGG-MEDIA-01: Upload image, document, generate-audio {#SR-AGG-MEDIA-01}

Загрузка медиа для Card Editor и Reader; TTS для audio fields (espeak/mistral через `ITtsAudioService`).

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Size limits** | Image max 5 MB; document max 50 MB — validation до gRPC. |
| **Content-type** | Image: `image/*` only; иначе 400. |
| **Multipart** | `IFormFile`, поле `file`. |
| **MediaService gRPC** | `UploadImage`, `UploadDocument` → public URL в MinIO bucket. |
| **TTS local/BFF** | `generate-audio` — синтез на Aggregator, provider из config. |
| **JWT + metadata** | userId, roles в gRPC MediaService calls. |

### 2. Высокоуровневое описание

Представим upload как **сканирование на стойке регистрации**.

1. **Editor/Reader** отправляет multipart file.
2. **Aggregator** проверяет size и MIME, читает bytes в memory stream.
3. **MediaService** сохраняет в `polyraspad-media`, возвращает URL для note field или book record.
4. **generate-audio:** BFF вызывает TTS (espeak в Docker / Mistral external), возвращает audio URL или bytes.

Aggregator **не** генерирует signed URLs logic beyond MediaService response.

Таким образом, все binary assets централизованы в MinIO, а BFF — validation gate + transport.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `MediaController`, base `/api/Media`.
* **Downstream upload:** `IMediaServiceClient`.

#### Сценарий А: Upload image для карточки (Happy Path)

**Сценарий:** Card Editor вставляет image из clipboard.

1. **POST** `/api/Media/upload-image`, multipart `file`, Bearer JWT.
2. **Validation (BFF):** size ≤ 5 MB, content-type image/*.
3. **gRPC:** `UploadImage` с byte payload.
4. **Ответ:** HTTP **201**, `{ url, … }` в DTO.

#### Сценарий Б: File too large (Negative Path)

1. **POST** upload-image > 5 MB.
2. **BFF:** HTTP **400** без gRPC.

#### Сценарий В: Generate TTS (Happy Path)

1. **POST** `/api/Media/generate-audio` с text/voice params.
2. **BFF:** `ITtsAudioService` synthesize.
3. **Ответ:** HTTP **200**, audio URL или stream.

---

## SR-AGG-MEDIA-02: Extract document text {#SR-AGG-MEDIA-02}

Локальное извлечение текста из PDF/EPUB/TXT — `IDocumentTextExtractor` на BFF, без отдельного NLP microservice.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **BFF-local** | Parsing не через gRPC MediaService. |
| **Reader import** | Подготовка plain text для library import + далее `POST /api/text/analyze`. |
| **Formats** | PDF, EPUB, TXT по реализации extractor. |
| **JWT** | Endpoint под `[Authorize]`. |

### 2. Высокоуровневое описание

Представим extract как **OCR-стойку в библиотеке**.

1. **Загрузка документа:** Клиент загрузил document (`upload-document`) или передаёт URL/reference.
2. **Парсинг на BFF:** Aggregator скачивает/читает файл и прогоняет через `IDocumentTextExtractor`.
3. **Ответ для UI:** Возвращает extracted text + metadata (title, chapters) для Reader Library.
4. **Preview анализа:** Frontend может вызвать analyze (группа 6) для term highlighting preview.

Таким образом, **import pipeline** split: extract на BFF, term analysis в Vocabulary.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Import EPUB (Happy Path)

**Сценарий:** Пользователь импортирует книгу в Reader Library.

1. **POST** `/api/Media/extract-document-text` с document reference/bytes.
2. **BFF:** local parse via extractor.
3. **Ответ:** HTTP **200**, extracted text chunks + metadata.

#### Сценарий Б: Unsupported/corrupt file (Negative Path)

1. **Extractor** throws or returns empty.
2. **Ответ (BFF):** HTTP **400** или **422** с error detail.

---

## SR-AGG-MEDIA-03: Serve-image / serve-document proxy {#SR-AGG-MEDIA-03}

Credentialed GET proxy к MinIO/public URLs — frontend загружает медиа с Bearer без CORS блокировки.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **JWT on proxy** | `[Authorize]` — только authenticated users. |
| **Query url** | Encoded source URL; BFF fetch server-side. |
| **CORS** | Explicit origins + `AllowCredentials` — [[16 - Платформенные контракты (Operations)#SR-AGG-OPS-02|SR-AGG-OPS-02]]. |
| **Stream response** | Binary stream с correct content-type. |

### 2. Высокоуровневое описание

Представим proxy как **окно выдачи с проверкой билета**.

1. **UI** не может `<img src="https://minio:9000/...">` из browser из-за CORS.
2. **UI** запрашивает `/api/Media/serve-image?url=…` с Bearer — same-origin to API.
3. **BFF** валидирует JWT, HttpClient fetch file, stream to client.
4. **Study/Editor** отображает image inline.

Таким образом, proxy — **security + CORS workaround**, не public CDN.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Preview image in study (Happy Path)

1. **GET** `/api/Media/serve-image?url={encodedMinioUrl}` + Bearer.
2. **BFF:** fetch + stream.
3. **Ответ:** HTTP **200**, image bytes.

#### Сценарий Б: Missing/invalid url (Negative Path)

1. **GET** без url или malformed.
2. **Ответ:** HTTP **400**.

---

## SR-AGG-MEDIA-04: Reader library {#SR-AGG-MEDIA-04}

Управление books и collections; sharing collections между users.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Books** | GET list, PUT update, DELETE `/api/Media/library/{projectId}/books/{bookId}`. |
| **Collections** | CRUD `/library/{projectId}/collections`. |
| **Share** | POST share, DELETE unshare collaborator. |
| **Shared inbox** | GET `/api/Media/library/shared-collections`. |
| **Coordination** | Library state split Media + Vocabulary по gRPC (см. REST doc 09). |

### 2. Высокоуровневое описание

Представим library как **личную полку книг с общими подборками**.

1. **Import book:** extract + save book record в project library.
2. **Collections:** группировка books (course units, reading lists).
3. **Share:** owner добавляет collaborator userId — shared read access.
4. **Collaborator:** видит shared collections через dedicated list endpoint.

Aggregator проксирует CRUD; reading progress/terms — в Vocabulary/Reader flows.

Таким образом, **library UX** централизован в Media REST, а **learning state** — в term/card domains.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Share collection (Happy Path)

**Сценарий:** Owner делится reading list с другом.

1. **POST** `/api/Media/library/{projectId}/collections/{collectionId}/share` + collaborator userId.
2. **Downstream:** share RPC.
3. **Ответ:** HTTP **200** или **204**.

#### Сценарий Б: List shared collections (Happy Path)

1. **GET** `/api/Media/library/shared-collections`.
2. **Ответ:** HTTP **200**, collections shared with current user.

---

*Следующая группа: [[10 - SaaS-биллинг (Billing)]].*
