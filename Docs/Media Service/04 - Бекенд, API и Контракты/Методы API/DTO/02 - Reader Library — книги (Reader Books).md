# Введение

DTO группы **Reader Books** — proto message `ReaderLibraryBook` и list/save/delete wrappers. Поля соответствуют [[03 - Модель Данных/01 - Основные сущности/Entity - Reader Library - Reader Library#Reader Library Book]].

# 1. Список DTO

| DTO | gRPC |
| :--- | :--- |
| `ReaderLibraryBook` | Все book RPC |
| `ListReaderLibraryBooksRequest` / `Response` | `#grpc-ListReaderLibraryBooks` |
| `SaveReaderLibraryBookRequest` / `Response` | `#grpc-SaveReaderLibraryBook` |
| `DeleteReaderLibraryBookRequest` / `Response` | `#grpc-DeleteReaderLibraryBook` |

---

<span id="dto-ReaderLibraryBook"></span>

# DTO: ReaderLibraryBook

## Контекст и назначение

Метаданные книги в library index и gRPC responses. Aggregator маппит тот же JSON в REST.

**Назначение:** Ответ / вложенный объект
**Реализация сущности:** `ReaderLibraryBookRecord` в S3 JSON

## Структура данных

| Имя поля (JSON) | Тип | Описание |
| :--- | :--- | :--- |
| `id` | `string` (UUID) | PK книги в индексе |
| `title` | `string` | Отображаемое название (required) |
| `file_name` | `string` | Исходное имя файла (required) |
| `url` | `string` | Resolved document URL (response only) |
| `document_id` | `string` (UUID) | Ссылка на `documents/{id}` |
| `page_count` | `int32` | Число страниц |
| `uploaded_at` | `string` | ISO 8601 |
| `last_opened_at` | `string` | ISO 8601 |
| `last_page_number` | `int32` | Прогресс Reader |
| `collection_id` | `string` | UUID коллекции или пусто |
| `collection_name` | `string` | Денормализованное имя |
| `is_shared` | `boolean` | true для collaborator view |
| `owner_user_id` | `string` (UUID) | Владелец индекса |
| `owner_user_name` | `string` | Display name |
| `owner_email` | `string` | Email |

## Пример работы (JSON)

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "The Great Gatsby",
  "file_name": "gatsby.epub",
  "url": "http://minio:9000/polyraspad-media/documents/...",
  "document_id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "page_count": 180,
  "last_page_number": 42,
  "uploaded_at": "2026-06-01T12:00:00Z",
  "last_opened_at": "2026-06-28T09:15:00Z",
  "collection_id": "",
  "collection_name": "",
  "is_shared": false,
  "owner_user_id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "owner_user_name": "Reader",
  "owner_email": "reader@example.com"
}
```
