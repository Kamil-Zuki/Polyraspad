# Введение

DTO группы **Reader Collections** — коллекции, collaborators и share inbox. Сущности: [[03 - Модель Данных/01 - Основные сущности/Entity - Reader Library - Reader Library]].

# 1. Список DTO

| DTO | gRPC |
| :--- | :--- |
| `ReaderCollection` | Collection RPCs |
| `ReaderCollectionCollaborator` | Share / unshare |
| `ListReaderCollectionsRequest` / `Response` | `#grpc-ListReaderCollections` |
| `SaveReaderCollectionRequest` / `Response` | `#grpc-SaveReaderCollection` |
| `ShareReaderCollectionRequest` / `Response` | `#grpc-ShareReaderCollection` |
| `ListSharedReaderCollectionsRequest` / `Response` | `#grpc-ListSharedReaderCollections` |

---

<span id="dto-ReaderCollection"></span>

# DTO: ReaderCollection

## Контекст и назначение

Reading list с nested books в list/save responses.

**Реализация сущности:** `ReaderCollectionRecord` в S3 JSON

## Структура данных

| Имя поля | Тип | Описание |
| :--- | :--- | :--- |
| `id` | `string` (UUID) | PK коллекции |
| `project_id` | `string` | Языковой project |
| `name` | `string` | Название (required) |
| `description` | `string` | Описание |
| `created_at` | `string` | ISO 8601 |
| `updated_at` | `string` | ISO 8601 |
| `owner_user_id` | `string` (UUID) | Владелец |
| `owner_user_name` | `string` | Display name |
| `owner_email` | `string` | Email |
| `collaborators` | `array<ReaderCollectionCollaborator>` | Share snapshot |
| `book_count` | `int32` | Derived в response |
| `is_shared_with_me` | `boolean` | true в shared inbox |
| `can_edit` | `boolean` | Из access record |
| `books` | `array<ReaderLibraryBook>` | Nested books |

---

<span id="dto-ReaderCollectionCollaborator"></span>

# DTO: ReaderCollectionCollaborator

## Структура данных

| Имя поля | Тип | Описание |
| :--- | :--- | :--- |
| `user_id` | `string` (UUID) | Collaborator (required) |
| `user_name` | `string` | Display name |
| `email` | `string` | Email |
| `can_edit` | `boolean` | Разрешение редактирования |
| `shared_at` | `string` | ISO 8601 |
