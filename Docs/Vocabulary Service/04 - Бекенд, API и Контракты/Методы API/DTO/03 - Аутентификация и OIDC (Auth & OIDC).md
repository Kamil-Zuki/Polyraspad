# DTO: Синхронизация и Маркетплейс

Данный документ описывает DTO дельта-синхронизации, офлайн-ответов и продуктов каталога Marketplace.

---

## 1. Synchronization DTOs

### `SyncDataRequest` / `SyncDataResponse`
- `last_sync_token` (`google.protobuf.Timestamp`): Временная метка последней успешной синхронизации.
- `sync_token` (`Timestamp`): Новый выданный токен синхронизации.
- `requires_full_sync` (bool): Флаг принудительной полной перезагрузки базы при несогласованности.
- `changes` (`SyncChanges`): Наборы измененных колод (`decks`), карточек (`cards`) и прогресса (`progress`).
- `deleted_objects` (repeated `DeletedObjectInfo`): Список удаленных сущностей (Tombstones).

### `BatchReviewItem`
- `card_id` (string): UUID карточки.
- `rating` (int): Оценка FSRS (1=Again, 2=Hard, 3=Good, 4=Easy).
- `reviewed_at` (`Timestamp`): Точное время ответа в офлайн-режиме.
- `duration_ms` (int): Длительность ответа в миллисекундах.

---

## 2. Marketplace & Community DTOs

### `ProductDto`
- `id` (string): UUID товара в каталоге.
- `deck_id` (string): Исходная опубликованная колода.
- `author_id` (string): UUID профиля автора.
- `title` (string), `description_html` (string).
- `price` (double), `currency` (string).
- `average_rating` (double), `review_count` (int), `sales_count` (int).

### `SubscriptionItemResponse`
- `deck_id` (string): UUID колоды.
- `project_id` (string): Проект пользователя.
- `subscribed_at` (`Timestamp`).
- `last_synced_version` (int): Версия скачанной колоды.
