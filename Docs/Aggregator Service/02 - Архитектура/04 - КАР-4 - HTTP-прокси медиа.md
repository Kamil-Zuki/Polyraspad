# Введение

HTTP-прокси `serve-image` и `serve-document` загружает файлы с MinIO/public URL server-side, чтобы браузер не обращался к storage напрямую (CORS, optional Bearer на image-by-id).

## Контекст и проблема

Reader и editor запрашивают медиа с credentialed requests; MinIO URL может быть недоступен из browser CORS policy.

## Принятое решение

1. GET `/api/Media/serve-image?id=` → gRPC GetImageUrl → HTTP GET upstream → stream response
2. GET `/api/Media/serve-document?url=` → validate URL → HTTP proxy
3. JWT required на controller level

## Обоснование и последствия

### Плюсы

* Единый origin для frontend
* Контроль доступа на gateway

### Последствия

* BFF несёт bandwidth proxy load
* *Решение:* nginx caching/CDN для production media paths

{#КАР-4}
