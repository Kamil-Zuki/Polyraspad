---
title: "Локальное хранилище медиа (MinIO / S3)"
tags: [polyraspad, docs, minio, s3, media]
---

# Локальное хранилище медиа (MinIO / S3)

VocabularyService сохраняет изображения и аудио в S3-совместимом хранилище. Для локальной разработки используется **MinIO**.

## Запуск MinIO (Docker Compose)

В корне репозитория:

```bash
docker compose up -d minio
```

MinIO будет доступен:

- **API:** http://localhost:9000
- **Console (опционально):** http://localhost:9001

Учётные данные по умолчанию: `minioadmin` / `minioadmin` (переменные `MINIO_ROOT_USER` и `MINIO_ROOT_PASSWORD`).

## Конфигурация VocabularyService

Секция **Storage** в `appsettings.json` или переменные окружения:

| Параметр | Описание | Пример (локально) |
|----------|----------|-------------------|
| `Storage:Endpoint` | URL MinIO | `http://localhost:9000` |
| `Storage:Bucket` | Имя бакета | `polyraspad-media` |
| `Storage:AccessKey` | Access key | `minioadmin` |
| `Storage:SecretKey` | Secret key | `minioadmin` |
| `Storage:UsePathStyle` | Path-style для MinIO | `true` |
| `Storage:PublicBaseUrl` | Базовый URL для доступа к объектам (если бакет public) | `http://localhost:9000/polyraspad-media` |
| `Storage:PresignedUrlExpirationMinutes` | Срок действия presigned URL (если PublicBaseUrl не задан) | `60` |

В Docker Compose для `vocabulary-service` эти переменные заданы через `environment` (в т.ч. `Storage__Endpoint`, `Storage__Bucket` и т.д.).

## Создание бакета

VocabularyService при первом обращении к хранилищу (например, при загрузке скриншота в CaptureCard) проверяет наличие бакета и создаёт его при необходимости. Отдельный init-скрипт не требуется.

Для доступа по постоянному URL (без presigned) бакет можно сделать публичным в MinIO Console (Bucket → Access Rules → read для `*`). Тогда `PublicBaseUrl` будет отдавать URL вида `http://localhost:9000/polyraspad-media/images/{id}`.

## Использование в коде

- **Загрузка:** `IMediaStorageService.UploadImageAsync` / `UploadAudioAsync` возвращают media ID (Guid), который сохраняется в `Card.Media.ImageId` / `AudioId`.
- **Reader (PDF):** через шлюз доступны `POST /api/Media/upload-document` и операции библиотеки (`/api/Media/library/...`); бинарники по-прежнему уходят в S3-совместимое хранилище (см. `MediaController` + MediaService).
- **Отдача URL:** при возврате карточки клиенту (GetCard, GetNextCard, список карточек и т.д.) сервис заполняет `Media.ImageUrl` и `Media.AudioUrl` через `FillCardMediaUrlsAsync` (по `PublicBaseUrl` или presigned URL).

## См. также

- [[Описание REST API]] — Capture Card, медиа в ответах
- [[Основные возможности]] — Media Service (S3), SR-BG-02 (очистка медиа)
