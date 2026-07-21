# Entity - Медиа и библиотека - Media

**Тип:** API Contract View

Downstream: `MediaService` (S3 + JSON indexes). Локально на BFF: extract-document-text (PDF/EPUB/TXT).

## Media upload / URL (контракт)

| Операция | Описание |
| :--- | :--- |
| upload-image / upload-document | Возвращает URL (public/presigned path) |
| generate-audio | TTS через Aggregator/Media |
| serve-image / serve-document | Same-origin proxy для credentialed preview |

Лимиты: image 5 MB, document 50 MB (как в MediaService).

## Reader library (контракт)

| Объект | Описание |
| :--- | :--- |
| Book | Id, title, fileName, pageCount, lastPage, collectionId, owner*, isShared |
| Collection | Id, projectId, name, description, collaborators |
| Share | share/unshare collaborator; list shared collections |

> Collections: GET/POST/DELETE (+ share) — полного PUT update коллекции в REST может не быть; сверять `MediaController`.

REST: `/api/Media/*`.
