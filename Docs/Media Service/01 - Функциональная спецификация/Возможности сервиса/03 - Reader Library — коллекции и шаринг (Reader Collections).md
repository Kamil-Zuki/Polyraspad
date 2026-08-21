# Группа 3: Reader Library — коллекции и шаринг (Reader Collections)

## Введение

В этом разделе описывается **CRUD коллекций** (reading lists) и **совместный доступ** — share/unshare collaborators и inbox расшаренных коллекций.

Индекс: `reader-collections/{userId}/{projectId}/index.json`. Share не копирует книги — collaborator читает owner index через scan при `ListSharedReaderCollections`.

**Метафора:**

Представьте **тематические подборки на полке с гостевым списком**. Владелец составляет список и даёт другу «пропуск смотреть эту подборку»; друг видит её в своём «входящих расшаренных».

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Reader Library — коллекции и шаринг (Reader Collections).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-MEDIA-COLL-01** | **List collections:** Коллекции owner в project + nested books. |
| **SR-MEDIA-COLL-02** | **Save collection:** Upsert name, description, collaborators snapshot. |
| **SR-MEDIA-COLL-03** | **Delete collection:** Удаление; книги проекта теряют `collection_id`. |
| **SR-MEDIA-COLL-04** | **Share collection:** Добавление collaborator с `can_edit`. |
| **SR-MEDIA-COLL-05** | **Unshare collection:** Удаление collaborator. |
| **SR-MEDIA-COLL-06** | **List shared collections:** Все коллекции, где caller в `collaborators`. |

---

# Детальная спецификация требований

## SR-MEDIA-COLL-01: List collections {#SR-MEDIA-COLL-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Enriched response** | Каждая collection включает books с matching `collection_id`. |
| **book_count** | Derived из списка книг. |

### 2. Высокоуровневое описание

Представим list collections как **просмотр тематических подборок на полке с книгами внутри**.

1. **Запрос UI:** Library collections tab вызывает gRPC `ListReaderCollections(project_id)` с header `user_id`.
2. **Чтение индекса:** Сервис читает `reader-collections/{userId}/{projectId}/index.json` owner.
3. **Обогащение книгами:** Каждая collection включает books с matching `collection_id`; `book_count` derived из списка книг.
4. **Единый ответ:** UI получает `collections[]` с nested `books[]` в одном gRPC round-trip.

Таким образом, UI показывает коллекции как expandable lists без дополнительных запросов на каждую подборку.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Library collections tab (Happy Path)

1. **gRPC:** `ListReaderCollections(project_id)`.
2. **Ответ:** `collections[]` с `books[]`.

---

## SR-MEDIA-COLL-02: Save collection {#SR-MEDIA-COLL-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Upsert** | По `collection.id` (UUID). |
| **project_id** | Required на collection payload. |
| **Owner** | `owner_user_id` = caller. |

### 2. Высокоуровневое описание

Представим save collection как **создание или переименование тематической подборки на полке**.

1. **Upsert payload:** Вызывается gRPC `SaveReaderCollection` с `collection.id` (UUID), `project_id` (required), name и optional description.
2. **Фиксация owner:** `owner_user_id` принудительно = caller `user_id`; collaborators snapshot сохраняется в payload.
3. **Создание или update:** Новый UUID — create reading list; существующий — rename/description update.
4. **Перезапись индекса:** JSON индекс коллекций перезаписывается в S3; в ответе — saved `ReaderCollection`.

Таким образом, owner управляет метаданными reading list без копирования книг — книги ссылаются на collection через `collection_id`.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Create collection (Happy Path)

1. **gRPC:** `SaveReaderCollection` с new UUID + name.
2. **Ответ:** saved `ReaderCollection`.

---

## SR-MEDIA-COLL-03: Delete collection {#SR-MEDIA-COLL-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Cascade metadata** | Books в project: `collection_id` cleared. |
| **Books persist** | Книги не удаляются. |

### 2. Высокоуровневое описание

Представим delete collection как **снятие подборки с полки без уничтожения книг**.

1. **Запрос удаления:** Owner вызывает gRPC `DeleteReaderCollection(project_id, collection_id)`.
2. **Удаление подборки:** Запись collection удаляется из индекса `reader-collections/{userId}/{projectId}/index.json`.
3. **Cascade metadata:** Books в project: `collection_id` cleared — книги возвращаются в «без коллекции».
4. **Обновление индексов:** S3: update both indices (collections и library books); сами книги не удаляются.

Таким образом, подборка исчезает из UI, а книги остаются в library owner без привязки к collection.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Delete reading list (Happy Path)

1. **gRPC:** `DeleteReaderCollection(project_id, collection_id)`.
2. **S3:** update both indices.

---

## SR-MEDIA-COLL-04: Share collection {#SR-MEDIA-COLL-04}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Collaborator payload** | `user_id`, optional name/email, `can_edit`. |
| **Upsert collaborator** | Same user_id replaces prior entry. |
| **updated_at** | Bump на share. |

### 2. Высокоуровневое описание

Представим share collection как **выдачу гостевого пропуска на просмотр тематической подборки**.

1. **Выбор collaborator:** Owner делится reading list с другом по `userId` (из профиля / поиска на BFF).
2. **Payload collaborator:** gRPC `ShareReaderCollection` принимает `user_id`, optional name/email, `can_edit`.
3. **Upsert collaborator:** Same `user_id` replaces prior entry; `updated_at` bump на share.
4. **Без копирования книг:** Share не копирует книги — collaborator читает owner index через scan при `ListSharedReaderCollections`.

Таким образом, друг получает доступ к подборке через snapshot collaborators в owner index, а не через дублирование blob.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Share with friend (Happy Path)

1. **gRPC:** `ShareReaderCollection` + collaborator.
2. **Ответ:** updated collection с collaborators.

#### Сценарий Б: Invalid collaborator (Negative Path)

1. **gRPC:** missing/invalid `collaborator.user_id`.
2. **Ответ:** `InvalidArgument`.

---

## SR-MEDIA-COLL-05: Unshare collection {#SR-MEDIA-COLL-05}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **collaborator_user_id** | UUID required. |
| **Owner only** | Caller = owner (enforced by owner index path). |

### 2. Высокоуровневое описание

Представим unshare collection как **отзыв гостевого пропуска с тематической подборки**.

1. **Запрос revoke:** Owner вызывает gRPC `UnshareReaderCollection` с `collaborator_user_id` (UUID required).
2. **Owner only:** Caller = owner; enforced by owner index path.
3. **Удаление из snapshot:** Collaborator удаляется из списка `collaborators` в owner index.
4. **Исчезновение из inbox:** Collaborator перестаёт видеть collection в shared inbox (`ListSharedReaderCollections`).

Таким образом, доступ к подборке отзывается без удаления книг и без копирования данных collaborator.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Revoke share (Happy Path)

1. **gRPC:** `UnshareReaderCollection` + `collaborator_user_id`.
2. **Ответ:** collection без collaborator.

---

## SR-MEDIA-COLL-06: List shared collections {#SR-MEDIA-COLL-06}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Cross-user scan** | List prefix `reader-collections/`, filter by collaborator match. |
| **is_shared_with_me** | true; `can_edit` from access record. |
| **Books** | Owner's books in collection, `is_shared=true`. |

### 2. Высокоуровневое описание

Представим list shared collections как **просмотр входящих расшаренных подборок без знания путей owner**.

1. **Запрос inbox:** Collaborator открывает «Shared with me» — gRPC `ListSharedReaderCollections` + caller `user_id`.
2. **Cross-user scan:** Сервис list prefix `reader-collections/`, filter by collaborator match в snapshot owner indices.
3. **Обогащение доступом:** `is_shared_with_me` = true; `can_edit` from access record; books — owner's books in collection, `is_shared=true`.
4. **Единый RPC:** Collaborator получает collections from other owners без знания owner project paths.

Таким образом, shared inbox работает одним RPC, не требуя от collaborator хранить или знать S3 key paths владельца.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Shared inbox (Happy Path)

1. **gRPC:** `ListSharedReaderCollections` + caller `user_id`.
2. **Ответ:** collections from other owners.

---

*Следующая группа: [[04 - Платформенные контракты (Operations)]].*
