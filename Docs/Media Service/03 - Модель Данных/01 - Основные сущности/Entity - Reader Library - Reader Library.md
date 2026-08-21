# Введение

Группа **Reader Library** описывает метаданные личной библиотеки читателя в контексте языкового **project**: книги (файлы), коллекции (подборки) и совместный доступ (collaborators).

Данные **не** в PostgreSQL: два JSON-индекса на пару `(owner_user_id, project_id)` в object storage:

- `reader-library/{userId}/{projectId}/index.json` — массив книг
- `reader-collections/{userId}/{projectId}/index.json` — массив коллекций с вложенными collaborators

Прогресс чтения (`last_page_number`, term statuses) — домен **VocabularyService** / Reader UX; Media Service хранит **каталог файлов и шаринг коллекций**.

---

# Reader Library Book (`ReaderLibraryBookRecord`)

## 1. Общее описание

Запись о книге/документе в библиотеке проекта: метаданные + ссылка на `documents/{document_id}` в object storage.

## 2. Атрибуты

| Название | Тип | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | NOT NULL | Идентификатор книги в индексе. |
| `document_id` | `uuid` | NULL | Ссылка на Media Object в `documents/`. |
| `title` | `string` | NOT NULL | Отображаемое название. |
| `file_name` | `string` | NOT NULL | Исходное имя файла при импорте. |
| `page_count` | `int` | NULL | Число страниц (если известно после extract на BFF). |
| `last_page_number` | `int` | NULL | Последняя открытая страница (UI sync). |
| `uploaded_at` | `string` | ISO 8601 | Время добавления в библиотеку. |
| `last_opened_at` | `string` | NULL, ISO 8601 | Время последнего открытия в Reader. |
| `collection_id` | `string` | NULL | UUID коллекции как строка; пусто — «без коллекции». |
| `collection_name` | `string` | NULL | Денормализованное имя коллекции для UI. |
| `owner_user_id` | `uuid` | NOT NULL | Владелец записи (владелец индекса). |
| `owner_user_name` | `string` | | Display name владельца. |
| `owner_email` | `string` | | Email владельца. |
| `is_shared` | `boolean` | default false | Книга из shared collection (для collaborator view). |

## 3. Связи

| Сущность | Тип связи |
| :--- | :--- |
| **Reader Collection** | N:1 по `collection_id` (логическая, без FK). |
| **Media Object (document)** | N:1 по `document_id`. |
| **Project** | Логическая: `project_id` в пути индекса (строка из Vocabulary). |

## 4. Жизненный цикл

1. **Import:** после `UploadDocument` на BFF/Aggregator — `SaveReaderLibraryBook` с metadata.
2. **Update:** `SaveReaderLibraryBook` upsert по `id` (progress, collection assignment).
3. **Delete:** удаление из JSON-индекса; blob в `documents/` **не** удаляется автоматически.
4. **Delete collection:** книги остаются, `collection_id` очищается.

---

# Reader Collection (`ReaderCollectionRecord`)

## 1. Общее описание

Подборка книг в Reader Library (reading list, course unit). Владелец — пользователь с индексом в `reader-collections/`.

## 2. Атрибуты (персистятся в JSON)

| Название | Тип | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | NOT NULL | Идентификатор коллекции. |
| `project_id` | `string` | NOT NULL | Языковой project (строка, sanitize в S3 key). |
| `name` | `string` | NOT NULL | Название подборки. |
| `description` | `string` | NULL | Описание. |
| `created_at` | `string` | ISO 8601 | Создание. |
| `updated_at` | `string` | ISO 8601 | Последнее изменение (share/unshare обновляет). |
| `owner_user_id` | `uuid` | NOT NULL | Владелец коллекции. |
| `owner_user_name` | `string` | | Display name. |
| `owner_email` | `string` | | Email. |
| `collaborators` | `array` | | Список [[#Reader Collection Collaborator]]. |

## 2.1. gRPC projection fields (не в JSON-индексе)

Поля proto `ReaderCollection`, вычисляемые при map в `MediaGrpcService` — **не персистятся**:

| Поле proto | Источник | Описание |
| :--- | :--- | :--- |
| `book_count` | `mappedBooks.Count` | Число книг коллекции в ответе. |
| `is_shared_with_me` | `access != null` | true если caller — collaborator (shared inbox / access record). |
| `can_edit` | `access?.CanEdit ?? true` | Для owner list без access → `true`; для shared — из collaborator. |
| `books[].url` | `GetMediaUrlForServerFetchAsync(document_id)` | Server-fetch URL документа книги; не хранится в index. |

## 3. Связи

| Сущность | Описание |
| :--- | :--- |
| **Reader Library Book** | Книги с `collection_id = collection.id` включаются в gRPC response. |
| **Collaborators** | 1:N вложенный массив в JSON. |
| **ReaderSharedCollectionRecord** | In-memory composite для shared inbox (не отдельный JSON файл). |

## 4. Жизненный цикл

CRUD через `SaveReaderCollection` / `DeleteReaderCollection`. При delete — книги в проекте теряют `collection_id`.

---

# Reader Shared Collection (`ReaderSharedCollectionRecord`)

## 1. Общее описание

**Не персистентная** composite-модель в коде (`MediaService.Models.ReaderSharedCollectionRecord`): результат scan shared inbox для `ListSharedReaderCollections`.

## 2. Атрибуты (in-memory)

| Название | Тип | Описание |
| :--- | :--- | :--- |
| `Collection` | `ReaderCollectionRecord` | Owner collection из чужого index.json. |
| `Books` | `IReadOnlyList<ReaderLibraryBookRecord>` | Книги этой коллекции из owner library index. |
| `Access` | `ReaderCollectionCollaboratorRecord?` | Запись collaborator текущего user (для `can_edit` / `is_shared_with_me`). |

Маппится в proto `ReaderCollection` с projection fields (`book_count`, `is_shared_with_me`, `can_edit`, book `url`).

---

# Reader Collection Collaborator (`ReaderCollectionCollaboratorRecord`)

## 1. Общее описание

Пользователь с доступом к **shared** коллекции. Не копия данных — записи в массиве `collaborators` у owner collection.

## 2. Атрибуты

| Название | Тип | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `user_id` | `uuid` | NOT NULL | Collaborator (получатель доступа). |
| `user_name` | `string` | | Display name. |
| `email` | `string` | | Email. |
| `can_edit` | `boolean` | | Разрешение редактирования (контракт; enforcement на BFF/UI). |
| `shared_at` | `string` | ISO 8601 | Время добавления в collaborators. |

## 3. Жизненный цикл

- **Share:** `ShareReaderCollection` — upsert collaborator.
- **Unshare:** `UnshareReaderCollection` — удаление из массива.
- **List shared:** `ListSharedReaderCollections` сканирует все `reader-collections/*/index.json`, находит записи где `collaborators` содержит `user_id` caller.

## 4. Ограничения реализации

Cross-user list — **O(n)** scan префикса `reader-collections/` в bucket. Для private beta объём допустим; при росте — ISSUE на индекс/БД.
