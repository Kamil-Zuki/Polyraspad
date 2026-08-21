# Введение

Метаданные **Reader Library** (книги и коллекции) хранятся как **JSON файлы** в том же bucket, а не в relational DB.

## Контекст и проблема

Reader Library — каталог документов per user per project с коллекциями и share lists. Для private beta объём мал; отдельная БД увеличивает ops footprint сервиса, который уже зависит от object storage.

## Принятое решение

1. **Books index:** `reader-library/{userId}/{projectId}/index.json` — массив `ReaderLibraryBookRecord`.
2. **Collections index:** `reader-collections/{userId}/{projectId}/index.json` — массив `ReaderCollectionRecord` с nested collaborators.
3. Read: `GetObject`; write: full JSON rewrite via `PutObject`.
4. **Share inbox:** `ListSharedReaderCollections` сканирует prefix `reader-collections/` и фильтрует collaborators.
5. Delete collection очищает `collection_id` на книгах owner в том же project.

## Обоснование и последствия

### Плюсы

* Нет миграций EF Core для library metadata.
* Backup = bucket backup.
* Простая mental model для dev Docker.

### Последствия

* Write amplification — каждый save перезаписывает entire index.
* Shared list scan O(all collection indices) — не для масштаба SaaS без redesign.
* *Решение:* ISSUE при росте; migration path to PostgreSQL documented in staging if needed.

*Связанные SR:* [[02 - Reader Library — книги (Reader Books)|SR-MEDIA-BOOK-*]], [[03 - Reader Library — коллекции и шаринг (Reader Collections)|SR-MEDIA-COLL-*]].

*Сущности:* [[Entity - Reader Library - Reader Library]].
