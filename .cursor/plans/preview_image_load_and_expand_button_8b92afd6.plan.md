---
name: Preview image load and expand button
overview: Исправление загрузки изображений в Preview через отдачу картинок с бэкенда (Aggregator) и улучшение видимости кнопки «Развернуть» модального окна.
todos: []
isProject: false
---

# План: загрузка фото в Preview (через бэкенд) и видимая кнопка разворота

## Проблема 1: изображение не загружается в Preview

**Причина:** Бэкенд возвращает URL хранилища (MinIO), запрос к нему идёт с другого origin; CORS или недоступность бакета приводят к ошибке загрузки и плейсхолдеру «Could not load image».

**Решение (правильное, с доработкой бэкенда):** Отдавать изображения через Aggregator: новый endpoint `GET /api/Media/serve-image` с авторизацией, который стримит картинку из хранилища. Фронт использует для превью только этот URL — один origin с API, без CORS и без доступа браузера к MinIO.

---

## 1. Бэкенд: VocabularyService

### 1.1 Идентификатор изображения в ответе UploadImage

- **Файлы:** [VocabularyService/Protos/vocabulary.proto](VocabularyService/Protos/vocabulary.proto), [AggregatorService/Protos/vocabulary.proto](AggregatorService/Protos/vocabulary.proto).
- В `UploadImageResponse` добавить поле `string image_id = 2;` (UUID загруженного изображения).
- В [VocabularyService/Grpc/CardGrpcService.cs](VocabularyService/Grpc/CardGrpcService.cs) в методе `UploadImage`: кроме `Url` возвращать `ImageId = imageId.ToString()`.

### 1.2 RPC GetImageUrl

- **Proto (оба сервиса):** добавить сообщения и метод:
  - `message GetImageUrlRequest { string image_id = 1; }`
  - `message GetImageUrlResponse { string url = 1; }`
  - `rpc GetImageUrl(GetImageUrlRequest) returns (GetImageUrlResponse);`
- **VocabularyService:** в [CardGrpcService](VocabularyService/Grpc/CardGrpcService.cs) реализовать `GetImageUrl`: разобрать `request.ImageId` в Guid, вызвать `_mediaStorage.GetMediaUrlAsync(imageId, "images", context.CancellationToken)`, вернуть URL. При невалидном id — InvalidArgument.

---

## 2. Бэкенд: AggregatorService

### 2.1 Клиент VocabularyService

- В [IVocabularyServiceClient](AggregatorService/Services/IVocabularyServiceClient.cs) и [VocabularyServiceClient](AggregatorService/Services/VocabularyServiceClient.cs) добавить метод `Task<GetImageUrlResponse> GetImageUrlAsync(string imageId, ...)` (userId/roles/cancellationToken по аналогии с другими вызовами). Вызов gRPC `GetImageUrl`.

### 2.2 DTO ответа загрузки

- В [UploadImageResponseDto](AggregatorService/Dtos/UploadImageResponseDto.cs) добавить свойство `string? ImageId`.
- В [MediaController](AggregatorService/Controllers/MediaController.cs) в `UploadImage`: в ответ подставлять `ImageId = response.ImageId` (если proto сгенерирует свойство с учётом `image_id`).

### 2.3 Endpoint отдачи изображения

- В [MediaController](AggregatorService/Controllers/MediaController.cs) добавить endpoint:
  - `GET /api/Media/serve-image`
  - Query-параметры: `id` (optional) — UUID изображения после загрузки; `url` (optional) — URL для прокси (для уже сохранённых карточек с imageUrl).
  - `[Authorize]`: только авторизованные пользователи.
  - Логика:
    - Если передан `id`: вызвать `GetImageUrlAsync(id)`, получить URL хранилища, выполнить server-side `HttpClient.GetAsync(url)` (без cookie), затем стримить ответ клиенту с тем же `Content-Type` и при необходимости `Cache-Control`.
    - Если передан только `url`: проверить, что URL разрешён (схема http/https и хост из whitelist — например конфигурируемый список или домен из настроек Storage/MinIO). Затем server-side fetch и стрим ответа.
  - Ошибки (невалидный id, недоступный url, таймаут): 404/502 с коротким телом.
- Зарегистрировать `HttpClient` для server-side запросов к MinIO, если ещё не зарегистрирован (для запроса по URL из GetImageUrl или из query).

---

## 3. Фронтенд

### 3.1 Типы и клиент загрузки

- В [media-client.ts](polyraspad-frontend/src/lib/api/media-client.ts): в типе `UploadImageResponse` добавить `imageId?: string`. После ответа от API использовать `response.imageId` при наличии.

### 3.2 Контекст редактора

- В [editor-card-context.tsx](polyraspad-frontend/src/contexts/editor-card-context.tsx): в состояние и контекст добавить опциональное поле `imageId: string` и сеттер (или положить в `setCardState`). При успешной загрузке изображения в форме выставлять `imageId` из ответа; при сбросе изображения — очищать.

### 3.3 Утилита URL для превью

- Новый файл, например [lib/utils/media-preview-url.ts](polyraspad-frontend/src/lib/utils/media-preview-url.ts) (или рядом с существующими утилитами):
  - Функция `getPreviewImageSrc(options: { imageId?: string; imageUrl?: string; apiBaseUrl: string }): string`.
  - Если передан `imageId` и `apiBaseUrl` — вернуть `${apiBaseUrl}/api/Media/serve-image?id=${imageId}`.
  - Иначе если передан `imageUrl`: если он с другого origin (или не начинается с apiBaseUrl), вернуть `${apiBaseUrl}/api/Media/serve-image?url=${encodeURIComponent(imageUrl)}`; иначе вернуть `imageUrl`.
  - `apiBaseUrl` брать из `process.env.NEXT_PUBLIC_API_URL` (или константы).
- Использовать только для формирования `src` в браузере; при SSR можно передавать пустой или не рендерить img до клиента.

### 3.4 Подстановка URL в превью

- В [card-preview.tsx](polyraspad-frontend/src/components/editor/card-preview.tsx): в `PreviewImage` и во всех местах, где выводится изображение из контекста, брать из контекста `imageUrl` и `imageId`, формировать `src` через `getPreviewImageSrc({ imageId, imageUrl, apiBaseUrl })`.
- В [editor-form.tsx](polyraspad-frontend/src/components/editor/editor-form.tsx): в блоке превью изображения (текущий `<img src={imageUrl.trim()}>`) подставлять тот же `getPreviewImageSrc` с `imageId` и `imageUrl` из контекста.

### 3.5 CORS

- Убедиться, что в Aggregator для `GET /api/Media/serve-image` разрешён origin фронтенда (как и для остальных API), чтобы запросы с браузера проходили.

---

## Проблема 2: кнопка раскрытия модального окна почти не видна

**Решение:** Увеличить контраст и размер кнопки «Развернуть» в [card-preview.tsx](polyraspad-frontend/src/components/editor/card-preview.tsx).

- Увеличить иконку: с `text-sm` до `text-base` или `text-lg`.
- Цвет: `text-brand-primary` (или `text-white/80`), в hover — `text-white` и при желании `bg-brand-primary/20`.
- Опционально: подпись «Full size» / «Развернуть» рядом с иконкой.
- Сохранить `title` и `aria-label`.

---

## Порядок внедрения

1. **VocabularyService:** proto (image_id в UploadImageResponse, GetImageUrl RPC), реализация в CardGrpcService (возврат ImageId, метод GetImageUrl).
2. **AggregatorService:** синхронизация proto, DTO (ImageId), GetImageUrlAsync в клиенте, endpoint GET serve-image (id и при необходимости url), регистрация HttpClient при необходимости.
3. **Фронт:** тип UploadImageResponse + imageId, контекст imageId, getPreviewImageSrc, подстановка в card-preview и editor-form.
4. Усилить видимость кнопки «Развернуть».

В результате превью всегда грузит изображения через бэкенд (по id после загрузки или по url для существующих карточек), без зависимости от CORS MinIO, а кнопка разворота модального окна хорошо заметна.
