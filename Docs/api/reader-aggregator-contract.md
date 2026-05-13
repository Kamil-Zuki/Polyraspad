# Reader ↔ Aggregator API Contract

**Версия:** v1.0-draft  
**Дата:** 2026-05-13  
**Статус:** В разработке (gRPC ready, REST missing)

## Endpoints Overview

| Endpoint | Method | Auth | Описание |
|----------|--------|------|----------|
| `/api/text/analyze` | POST | Bearer | Анализ текста, токенизация |
| `/api/terms` | POST | Bearer | Создать/обновить термин |
| `/api/terms/mark-known` | POST | Bearer | Пометить как известный |
| `/api/terms/ignore` | POST | Bearer | Игнорировать термин |
| `/api/terms/bulk-known` | POST | Bearer | Массовая пометка |
| `/api/terms/details` | GET | Bearer | Детали термина |
| `/api/terms/search-duplicates` | POST | Bearer | Поиск дубликатов |
| `/api/Media/library/{projectId}` | GET | Bearer | Список книг |
| `/api/Media/upload-document` | POST | Bearer | Загрузка PDF |
| `/api/Media/serve-document` | GET | Bearer | Получить файл |

---

## Text Analysis

### POST `/api/text/analyze`

**Request:**
```json
{
  "text": "The quick brown fox jumps over the lazy dog.",
  "projectId": "550e8400-e29b-41d4-a716-446655440000",
  "language": "en",
  "contextUrl": "optional-source-url",
  "contextTitle": "optional-title"
}
```

**Response 200:**
```json
{
  "tokens": [
    {
      "index": 0,
      "text": "The",
      "type": "WORD",
      "status": "KNOWN",
      "normalizedText": "the",
      "isStopWord": true
    },
    {
      "index": 1,
      "text": "quick",
      "type": "WORD",
      "status": "NEW",
      "normalizedText": "quick",
      "termId": null
    }
  ],
  "phrases": [
    {
      "startIndex": 1,
      "endIndex": 3,
      "text": "quick brown fox",
      "status": "LINGQ",
      "termId": "phrase-123"
    }
  ],
  "stats": {
    "totalTokens": 9,
    "uniqueWords": 8,
    "newCount": 3,
    "knownCount": 4,
    "learningCount": 1
  }
}
```

**Response 400:**
```json
{
  "error": "InvalidRequest",
  "message": "Text is too long (max 100000 characters)"
}
```

**Response 429:**
```json
{
  "error": "TooManyRequests",
  "message": "Rate limit exceeded for text analysis"
}
```

---

## Term Operations

### POST `/api/terms`

Создание или обновление термина (LingQ).

**Request:**
```json
{
  "text": "quick",
  "normalizedText": "quick",
  "type": "WORD", // или "PHRASE"
  "projectId": "550e8400-e29b-41d4-a716-446655440000",
  "meaning": "быстрый",
  "firstSentence": "The quick brown fox...",
  "firstSourceTitle": "Example Book",
  "firstSourceUrl": "optional-url",
  "status": "LINGQ" // NEW | LINGQ | KNOWN | IGNORED
}
```

**Response 201:**
```json
{
  "id": "term-123",
  "text": "quick",
  "normalizedText": "quick",
  "type": "WORD",
  "status": "LINGQ",
  "meaning": "быстрый",
  "createdAt": "2026-05-13T10:00:00Z",
  "updatedAt": "2026-05-13T10:00:00Z"
}
```

### POST `/api/terms/mark-known`

**Request:**
```json
{
  "termId": "term-123",
  "projectId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response 200:**
```json
{
  "id": "term-123",
  "status": "KNOWN",
  "updatedAt": "2026-05-13T10:05:00Z"
}
```

### POST `/api/terms/ignore`

Аналогично mark-known, но статус IGNORED.

### POST `/api/terms/bulk-known`

Массовая операция для page turn.

**Request:**
```json
{
  "termIds": ["term-1", "term-2", "term-3"],
  "projectId": "550e8400-e29b-41d4-a716-446655440000",
  "context": {
    "type": "PAGE_TURN",
    "pageNumber": 5,
    "bookId": "book-123"
  }
}
```

**Response 200:**
```json
{
  "updatedCount": 3,
  "failedCount": 0,
  "terms": [
    {"id": "term-1", "status": "KNOWN"},
    {"id": "term-2", "status": "KNOWN"},
    {"id": "term-3", "status": "KNOWN"}
  ]
}
```

### GET `/api/terms/details`

**Query Parameters:**
- `termId` (string, required)
- `projectId` (string, required)

**Response 200:**
```json
{
  "id": "term-123",
  "text": "quick",
  "normalizedText": "quick",
  "type": "WORD",
  "status": "LINGQ",
  "meaning": "быстрый",
  "firstSentence": "The quick brown fox...",
  "firstSourceTitle": "Example Book",
  "contexts": [
    {
      "sentence": "The quick brown fox jumps...",
      "sourceTitle": "Example Book",
      "sourceUrl": "...",
      "timestamp": "2026-05-13T10:00:00Z"
    }
  ],
  "cards": [
    {
      "id": "card-456",
      "targetWord": "quick",
      "deckTitle": "English Basics"
    }
  ]
}
```

### POST `/api/terms/search-duplicates`

**Request:**
```json
{
  "text": "quick",
  "normalizedText": "quick",
  "type": "WORD",
  "projectId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response 200:**
```json
{
  "exactMatches": [
    {
      "termId": "term-123",
      "text": "quick",
      "status": "LINGQ"
    }
  ],
  "similarTerms": [
    {
      "termId": "term-124",
      "text": "quickly",
      "status": "KNOWN"
    }
  ],
  "cards": [
    {
      "cardId": "card-456",
      "targetWord": "quick",
      "deckTitle": "English Basics"
    }
  ]
}
```

---

## Media Library

### GET `/api/Media/library/{projectId}`

**Response 200:**
```json
{
  "books": [
    {
      "id": "book-123",
      "title": "The Great Gatsby",
      "author": "F. Scott Fitzgerald",
      "language": "en",
      "totalPages": 156,
      "lastPageNumber": 42,
      "coverImageUrl": "...",
      "addedAt": "2026-01-15T10:00:00Z",
      "lastOpenedAt": "2026-05-13T09:00:00Z",
      "stats": {
        "totalUniqueWords": 5234,
        "knownWords": 2341,
        "learningWords": 150
      }
    }
  ],
  "collections": [
    {
      "id": "coll-1",
      "name": "English Classics",
      "bookCount": 5,
      "shared": false
    }
  ]
}
```

### POST `/api/Media/upload-document`

Content-Type: `multipart/form-data`

**Fields:**
- `file` (required): PDF/EPUB файл
- `projectId` (required): string
- `title` (optional): string
- `language` (optional): string

**Response 201:**
```json
{
  "id": "book-456",
  "title": "Uploaded Book",
  "totalPages": 200,
  "status": "processing", // или "ready"
  "message": "Document uploaded successfully. Processing..."
}
```

### GET `/api/Media/serve-document`

**Query Parameters:**
- `bookId` (string, required)
- `page` (number, optional): для PDF

**Response:** Binary stream (PDF) или JSON (text content)

---

## Error Handling

### Common Error Codes

| Code | HTTP | Описание |
|------|------|----------|
| InvalidRequest | 400 | Невалидные параметры |
| Unauthorized | 401 | Отсутствует или невалидный токен |
| Forbidden | 403 | Нет доступа к ресурсу |
| NotFound | 404 | Ресурс не найден |
| RateLimited | 429 | Превышен rate limit |
| InternalError | 500 | Внутренняя ошибка сервера |

### Error Response Format

```json
{
  "error": "ErrorCode",
  "message": "Human readable description",
  "details": {
    "field": "specific field with error",
    "hint": "how to fix"
  },
  "requestId": "uuid-for-tracing"
}
```

---

## Idempotency

Для операций изменения состояния (POST/PUT) поддерживается idempotency key:

**Header:** `Idempotency-Key: unique-uuid`

Повторный запрос с тем же ключом вернет тот же результат без побочных эффектов.

**Response Header:** `Idempotency-Key-Stored: true`

---

## Rate Limiting

| Endpoint | Limit | Window |
|----------|-------|--------|
| `/api/text/analyze` | 30 | 1 минута |
| `/api/terms/*` | 60 | 1 минута |
| `/api/Media/upload-document` | 10 | 1 минута |
| Остальные | 120 | 1 минута |

**Response Headers:**
```
X-RateLimit-Limit: 30
X-RateLimit-Remaining: 28
X-RateLimit-Reset: 1715601600
```

---

## Compatibility

- **Current Version:** v1
- **Breaking Changes:** Все breaking changes будут в v2 с префиксом `/api/v2/`
- **Deprecation:** Deprecated endpoints помечаются заголовком `Sunset: date`
