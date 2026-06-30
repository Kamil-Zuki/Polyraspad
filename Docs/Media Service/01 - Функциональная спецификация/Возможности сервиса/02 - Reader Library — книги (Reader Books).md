# Группа 2: Reader Library — книги (Reader Books)

## Введение

В этом разделе описывается управление **метаданными книг** Reader Library в контексте `project_id` и `user_id` (owner индекса).

Данные хранятся в JSON-файле `reader-library/{userId}/{projectId}/index.json`. Бинарный файл книги — отдельный Media Object в `documents/`.

**Метафора:**

Представьте **каталожную картотеку читального зала** — карточки с названием, полкой (collection) и номером страницы, на которой читатель остановился. Сами книги стоят в складе (documents/).

Identity: gRPC header `user_id` — [[04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-02|SR-MEDIA-OPS-02]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Reader Library — книги (Reader Books).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-MEDIA-BOOK-01** | **List library books:** Все книги owner в project с resolved document URL. |
| **SR-MEDIA-BOOK-02** | **Save library book:** Upsert title, document link, progress, collection assignment. |
| **SR-MEDIA-BOOK-03** | **Delete library book:** Удаление записи из индекса (blob не удаляется). |

---

# Детальная спецификация требований

## SR-MEDIA-BOOK-01: List library books {#SR-MEDIA-BOOK-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Scoped** | `project_id` required; books только owner `user_id`. |
| **Sort** | По `last_opened_at` / `uploaded_at`, затем title. |
| **URL hydration** | Для каждой книги с `document_id` — resolve URL в response. |

### 2. Высокоуровневое описание

Представим list library books как **открытие каталожной картотеки читального зала**.

1. **Запрос списка:** Reader Library UI запрашивает список при входе в project library; Aggregator → gRPC `ListReaderLibraryBooks(project_id)` с header `user_id`.
2. **Фильтрация по owner:** Сервис читает индекс `reader-library/{userId}/{projectId}/index.json` — только книги owner `user_id`.
3. **Сортировка и обогащение:** Книги сортируются по `last_opened_at` / `uploaded_at`, затем title; для каждой с `document_id` resolve URL в response.
4. **Ответ UI:** Возвращается массив `ReaderLibraryBook` с `url` полями для sidebar library.

Таким образом, UI получает полный каталог книг проекта с resolved document URL в одном gRPC round-trip.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Open library sidebar (Happy Path)

1. **gRPC:** `ListReaderLibraryBooks(project_id)` + `user_id` header.
2. **Ответ:** массив `ReaderLibraryBook` с `url` полями.

---

## SR-MEDIA-BOOK-02: Save library book {#SR-MEDIA-BOOK-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Upsert** | По `book.id` (UUID required). |
| **Required fields** | `title`, `file_name`. |
| **Owner** | `owner_user_id` принудительно = caller `user_id`. |
| **Progress** | `last_page_number`, `last_opened_at` для Reader resume. |

### 2. Высокоуровневое описание

Представим save library book как **обновление карточки в картотеке с номером страницы закладки**.

1. **Триггер save:** После import или при смене страницы Reader вызывает gRPC `SaveReaderLibraryBook` с book payload и `document_id`.
2. **Upsert по id:** Запись upsert по `book.id` (UUID required); обязательны `title`, `file_name`; `owner_user_id` принудительно = caller `user_id`.
3. **Прогресс чтения:** Сохраняются `last_page_number`, `last_opened_at` для Reader resume и optional `collection_id`.
4. **Перезапись индекса:** Полный JSON индекс перезаписывается в S3; в ответе — saved book с resolved `url`.

Таким образом, метаданные книги и прогресс чтения persist'ятся в owner index, а бинарный blob остаётся в `documents/`.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Save after import (Happy Path)

1. **gRPC:** `SaveReaderLibraryBook` с book payload + `document_id`.
2. **S3:** rewrite `index.json`.
3. **Ответ:** saved book с `url`.

#### Сценарий Б: Missing title (Negative Path)

1. **gRPC:** book без `title`.
2. **Ответ:** `InvalidArgument`.

---

## SR-MEDIA-BOOK-03: Delete library book {#SR-MEDIA-BOOK-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Index only** | Удаляется записи в JSON; `documents/{id}` остаётся. |
| **book_id** | Valid UUID required. |

### 2. Высокоуровневое описание

Представим delete library book как **изъятие карточки из картотеки без уничтожения книги на складе**.

1. **Запрос удаления:** User удаляет книгу из библиотеки; Aggregator вызывает gRPC `DeleteReaderLibraryBook(project_id, book_id)`.
2. **Валидация id:** `book_id` — valid UUID required.
3. **Index only:** Удаляется запись в JSON-индексе; blob `documents/{id}` остаётся в MinIO.
4. **Разделение доменов:** Термины и прогресс в Vocabulary — отдельный домен (не Media).

Таким образом, книга исчезает из UI library owner, но бинарный документ не удаляется автоматически.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Remove book (Happy Path)

1. **gRPC:** `DeleteReaderLibraryBook(project_id, book_id)`.
2. **Ответ:** empty success.

---

*Следующая группа: [[03 - Reader Library — коллекции и шаринг (Reader Collections)]].*
