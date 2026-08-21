# Введение

Media upload, TTS, document extraction, HTTP proxy, Reader Library. JWT. Downstream: **MediaService** + local BFF (`DocumentTextExtractor`, `TtsAudioService`). Сверено с `AggregatorService/Controllers/MediaController.cs`.

# 1. Список эндпоинтов

| SR | Method | Route | Downstream |
| :--- | :--- | :--- | :--- |
| SR-AGG-MEDIA-01 | POST | `/api/Media/upload-image` | gRPC `UploadImage` (max 5 MB) |
| SR-AGG-MEDIA-01 | POST | `/api/Media/generate-audio` | TTS → gRPC `UploadAudio` |
| SR-AGG-MEDIA-01 | POST | `/api/Media/upload-document` | gRPC `UploadDocument` (max 50 MB) |
| SR-AGG-MEDIA-02 | POST | `/api/Media/extract-document-text` | `IDocumentTextExtractor` (local) |
| SR-AGG-MEDIA-03 | GET | `/api/Media/serve-document` | HTTP proxy by URL |
| SR-AGG-MEDIA-03 | GET | `/api/Media/serve-image` | gRPC `GetImageUrl` + HTTP proxy |
| SR-AGG-MEDIA-04 | GET | `/api/Media/library/{projectId}` | gRPC `ListReaderLibraryBooks` |
| SR-AGG-MEDIA-04 | PUT | `/api/Media/library/{projectId}/books/{bookId}` | gRPC `SaveReaderLibraryBook` |
| SR-AGG-MEDIA-04 | DELETE | `/api/Media/library/{projectId}/books/{bookId}` | gRPC `DeleteReaderLibraryBook` |
| SR-AGG-MEDIA-04 | GET | `/api/Media/library/{projectId}/collections` | gRPC `ListReaderCollections` |
| SR-AGG-MEDIA-04 | POST | `/api/Media/library/{projectId}/collections` | gRPC `SaveReaderCollection` |
| SR-AGG-MEDIA-04 | DELETE | `/api/Media/library/{projectId}/collections/{collectionId}` | gRPC `DeleteReaderCollection` |
| SR-AGG-MEDIA-04 | POST | `/api/Media/library/{projectId}/collections/{collectionId}/share` | gRPC `ShareReaderCollection` + Auth `FindUserByEmail` |
| SR-AGG-MEDIA-04 | DELETE | `/api/Media/library/{projectId}/collections/{collectionId}/share/{collaboratorUserId}` | gRPC `UnshareReaderCollection` |
| SR-AGG-MEDIA-04 | GET | `/api/Media/library/shared-collections` | gRPC `ListSharedReaderCollections` |

См. [[04 - КАР-4 - HTTP-прокси медиа]].

---

# SR-AGG-MEDIA-01: Upload image: POST /api/Media/upload-image

## Общая информация

Загрузка изображения для Card Editor (multipart `file`).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `multipart/form-data` (`file`) |
| **DTO успешного ответа** | `UploadImageResponseDto` (`url`, `imageId`) |

## Логика обработки запроса

* Validate: file required, ≤ 5 MB, `content-type` image/*
* JWT → metadata user_id + roles
* gRPC [`UploadImage`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Загрузка%20и%20выдача%20медиа%20(Media%20Storage).md#grpc-UploadImage)
* HTTP **201** + JSON body (не `CreatedAtAction` — иначе пустое тело)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing file / size / content-type |
| **401** | JWT |
| **502** | gRPC error |
| **503** | Media storage unavailable |

---

# SR-AGG-MEDIA-01: Generate audio: POST /api/Media/generate-audio

## Общая информация

TTS через внешний провайдер + сохранение в MediaService.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `GenerateAudioRequestDto` |
| **DTO успешного ответа** | `GenerateAudioResponseDto` |

## Логика обработки запроса

* `ITtsAudioService.GenerateAndStoreAsync` → gRPC [`UploadAudio`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Загрузка%20и%20выдача%20медиа%20(Media%20Storage).md#grpc-UploadAudio)
* HTTP **201**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Validation |
| **502** | TTS provider / gRPC |
| **503** | TTS disabled |

---

# SR-AGG-MEDIA-04: Reader library: GET /api/Media/library/{projectId}

## Общая информация

Список книг Reader Library для project.

| Тип метода | GET |
| :--- | :--- |
| **DTO успешного ответа** | `IEnumerable<ReaderLibraryBookDto>` |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| `projectId` | string | UUID project |

## Логика обработки запроса

* JWT → metadata
* gRPC [`ListReaderLibraryBooks`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/02%20-%20Reader%20Library%20—%20книги%20(Reader%20Books).md#grpc-ListReaderLibraryBooks)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing projectId |
| **502** | Downstream |

---

# SR-AGG-MEDIA-04: Share collection: POST /api/Media/library/{projectId}/collections/{collectionId}/share

## Общая информация

Шаринг коллекции Reader Library по email collaborator.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `ShareReaderCollectionDto` (`email`, `canEdit`) |
| **DTO успешного ответа** | `ReaderCollectionDto` |

## Логика обработки запроса

* gRPC Auth [`FindUserByEmail`](../../../../Authorization%20Module/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Управление%20профилем%20(Profile%20Management).md#grpc-FindUserByEmail) по email
* gRPC [`ShareReaderCollection`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Reader%20Library%20—%20коллекции%20и%20шаринг%20(Reader%20Collections).md#grpc-ShareReaderCollection)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing email / user not found |
| **502** | Downstream |

---

# SR-AGG-MEDIA-01: Upload document: POST /api/Media/upload-document

## Общая информация

Загрузка PDF/EPUB/TXT для Reader Library (max **50 MB**).

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | multipart `file` |
| **DTO успешного ответа** | `UploadDocumentResponseDto` (`url`, `documentId`) |

## Логика обработки запроса

* Validate format: PDF, EPUB, TXT
* gRPC [`UploadDocument`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Загрузка%20и%20выдача%20медиа%20(Media%20Storage).md#grpc-UploadDocument)
* HTTP **201**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing file / size / unsupported format |
| **502** | gRPC / storage unavailable |

---

# SR-AGG-MEDIA-02: Extract text: POST /api/Media/extract-document-text

## Общая информация

Локальный parse документа на BFF (`IDocumentTextExtractor`) для import preview.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | document reference или upload |
| **DTO успешного ответа** | plain text + metadata |

## Логика обработки запроса

* Fetch blob или принять upload
* In-process extract (без gRPC persist)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Unsupported format |
| **502** | Fetch failed |

---

# SR-AGG-MEDIA-03: Serve document: GET /api/Media/serve-document

## Общая информация

HTTP proxy документа по signed/server URL (CORS-safe для Reader).

| Тип метода | GET |
| :--- | :--- |
| **Query** | `url` (encoded storage URL) |

## Логика обработки запроса

* Validate URL against allowed MinIO base
* Stream bytes to client

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing/invalid url |
| **502** | Upstream fetch failed |

---

# SR-AGG-MEDIA-03: Serve image: GET /api/Media/serve-image

## Общая информация

Proxy изображения: optional gRPC `GetImageUrl` + HTTP stream.

| Тип метода | GET |
| :--- | :--- |
| **Query** | `imageId` or `url` |

## Логика обработки запроса

* Resolve URL via [`GetImageUrl`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/01%20-%20Загрузка%20и%20выдача%20медиа%20(Media%20Storage).md#grpc-GetImageUrl) if needed
* Proxy response with cache headers

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Image not found |
| **502** | Proxy error |

---

# SR-AGG-MEDIA-04: Save book: PUT /api/Media/library/{projectId}/books/{bookId}

## Общая информация

Create/update книги Reader Library (metadata + document link).

| Тип метода | PUT |
| :--- | :--- |
| **DTO запроса** | `SaveReaderLibraryBookDto` |
| **DTO успешного ответа** | `ReaderLibraryBookDto` |

## Логика обработки запроса

* gRPC [`SaveReaderLibraryBook`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/02%20-%20Reader%20Library%20—%20книги%20(Reader%20Books).md#grpc-SaveReaderLibraryBook)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Validation |
| **502** | Downstream |

---

# SR-AGG-MEDIA-04: Delete book: DELETE /api/Media/library/{projectId}/books/{bookId}

## Общая информация

Удаление книги из library project scope.

| Тип метода | DELETE |
| :--- | :--- |
| **DTO успешного ответа** | N/A |

## Логика обработки запроса

* gRPC [`DeleteReaderLibraryBook`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/02%20-%20Reader%20Library%20—%20книги%20(Reader%20Books).md#grpc-DeleteReaderLibraryBook)
* HTTP **204**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **404** | Book not found |
| **502** | Downstream |

---

# SR-AGG-MEDIA-04: List collections: GET /api/Media/library/{projectId}/collections

## Общая информация

Список коллекций Reader Library в project.

| Тип метода | GET |
| :--- | :--- |
| **DTO успешного ответа** | `ReaderCollectionDto[]` |

## Логика обработки запроса

* gRPC [`ListReaderCollections`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Reader%20Library%20—%20коллекции%20и%20шаринг%20(Reader%20Collections).md#grpc-ListReaderCollections)

---

# SR-AGG-MEDIA-04: Save collection: POST /api/Media/library/{projectId}/collections

## Общая информация

Создание или обновление коллекции.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | `SaveReaderCollectionDto` |

## Логика обработки запроса

* gRPC [`SaveReaderCollection`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Reader%20Library%20—%20коллекции%20и%20шаринг%20(Reader%20Collections).md#grpc-SaveReaderCollection)

---

# SR-AGG-MEDIA-04: Delete collection: DELETE /api/Media/library/{projectId}/collections/{collectionId}

## Общая информация

Удаление коллекции.

| Тип метода | DELETE |

## Логика обработки запроса

* gRPC [`DeleteReaderCollection`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Reader%20Library%20—%20коллекции%20и%20шаринг%20(Reader%20Collections).md#grpc-DeleteReaderCollection)
* HTTP **204**

---

# SR-AGG-MEDIA-04: Unshare collection: DELETE …/share/{collaboratorUserId}

## Общая информация

Отзыв доступа collaborator к коллекции.

| Тип метода | DELETE |

## Логика обработки запроса

* gRPC [`UnshareReaderCollection`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Reader%20Library%20—%20коллекции%20и%20шаринг%20(Reader%20Collections).md#grpc-UnshareReaderCollection)

---

# SR-AGG-MEDIA-04: Shared collections: GET /api/Media/library/shared-collections

## Общая информация

Коллекции, расшаренные текущему пользователю.

| Тип метода | GET |
| :--- | :--- |
| **DTO успешного ответа** | shared collection list |

## Логика обработки запроса

* gRPC [`ListSharedReaderCollections`](../../../../Media%20Service/04%20-%20Бекенд,%20API%20и%20Контракты/Методы%20API/gRPC/03%20-%20Reader%20Library%20—%20коллекции%20и%20шаринг%20(Reader%20Collections).md#grpc-ListSharedReaderCollections)
