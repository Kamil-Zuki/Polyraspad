# Введение

URL бинарных объектов резолвятся через **два optional base URL** или **presigned S3 URL** — разделение browser access и backend fetch.

## Контекст и проблема

* Browser нужен URL через nginx (`MINIO_PUBLIC_BASE_URL` / `api.polyraspad.online/polyraspad-media/`).
* Aggregator `serve-image` HttpClient нужен URL reachable **inside Docker** (`MINIO_SERVER_FETCH_BASE_URL` → `http://minio:9000/...`).
* Presigned URLs — fallback когда public base не настроен.

## Принятое решение

1. **`GetMediaUrlAsync`** — для upload response: `PublicBaseUrl` + key, else presigned.
2. **`GetMediaUrlForServerFetchAsync`** — priority: `ServerFetchBaseUrl` → `PublicBaseUrl` → presigned.
3. Library book mapping использует server-fetch для `url` field в gRPC response.
4. Presigned TTL: `PresignedUrlExpirationMinutes` (default 60).

## Обоснование и последствия

### Плюсы

* CORS и same-origin proxy на BFF без exposing MinIO credentials to browser.
* Dev Docker works with path-style MinIO.

### Последствия

* Presigned URLs expire — long-lived UI may need refresh or public URL in prod.
* *Решение:* production nginx public bucket path; presigned только dev.

*Связанные SR:* [[01 - Загрузка и выдача медиа (Media Storage)#SR-MEDIA-STORAGE-04|SR-MEDIA-STORAGE-04..06]].

*Aggregator КАР:* Aggregator [[02 - КАР-4 - HTTP-прокси медиа]].
