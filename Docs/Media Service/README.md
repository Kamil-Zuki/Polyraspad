# Media Service — документация микросервиса

**Media Service** — внутренний gRPC-микросервис объектного хранилища и метаданных **Reader Library** платформы Polyraspad. Публичный REST — через **AggregatorService** (`/api/Media/*`); межсервисный контракт — **gRPC** (порт `5121`, h2c в Docker).

Данные: **S3-совместимое хранилище** (MinIO, bucket `polyraspad-media`) — бинарные медиа и JSON-индексы библиотеки. PostgreSQL, Redis и RabbitMQ **не используются**.

## Структура

| Папка | Содержание |
| :--- | :--- |
| [[01 - Функциональная спецификация]] | SR-группы и сервисные требования (`SR-MEDIA-*`) |
| [[02 - Архитектура]] | КАР, слои, интеграции |
| [[03 - Модель Данных]] | Объекты S3 и JSON-индексы Reader Library |
| [[04 - Бекенд, API и Контракты]] | gRPC (16 RPC), DTO, MinIO, алгоритмы |
| [[99 - Staging — Разрывы согласованности (DO NOT DELETE)]] | ISSUE при расхождениях `01`↔`03` |

## Эталон формата

- Полный образец: `(Done) Authorization Service/` — **layout only**
- Правила: `Docs/.cursor/rules/steos-docs-*`

## Код

Реализация: `MediaService/` (.NET 8, gRPC `5121`, AWS SDK S3 → MinIO).

## Клиенты

| Клиент | Транспорт | Назначение |
| :--- | :--- | :--- |
| AggregatorService | gRPC + REST BFF | Upload, library CRUD, URL resolution для UI |
| VocabularyService | gRPC (косвенно) | Использует media URLs в карточках; library import через Aggregator |

## Отличия от BFF

- **TTS (`generate-audio`)**, **extract-document-text**, **serve-image/serve-document** — на **Aggregator**, не в Media Service.
- Media Service отвечает только за **persist binary** и **Reader Library metadata** в object storage.
