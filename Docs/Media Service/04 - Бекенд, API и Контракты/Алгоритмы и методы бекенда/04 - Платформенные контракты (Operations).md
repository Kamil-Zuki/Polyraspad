# Платформенные контракты (Operations)

# 1. Список алгоритмов

| Алгоритм | SR | Описание |
| :--- | :--- | :--- |
| Извлечение `user_id` из gRPC metadata | SR-MEDIA-OPS-02 | Parse header → Guid или `UNAUTHENTICATED` |
| Health liveness | SR-MEDIA-OPS-01 | Minimal HTTP endpoint без S3 probe |

---

# Алгоритм извлечения user_id из gRPC metadata

## Контекст и область применения

### Почему был создан

Media Service — gRPC-only internal microservice без JWT middleware. Identity приходит от trusted BFF (Aggregator) через metadata.

### Бизнес-требование

SR-MEDIA-OPS-02

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Все Reader Library RPC (books + collections + share) |
| 2 | Не применяется к Upload/Get*Url RPC |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Media не проверяет подпись JWT — network policy + single caller |
| 2 | Forged `user_id` от untrusted caller = data isolation breach |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `context.RequestHeaders["user_id"]` | string | UUID пользователя | Да (library RPC) |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `userId` | Guid | Parsed owner/caller identity |

## Логика работы (Псевдокод)

```csharp
// MediaGrpcService.GetRequiredUserId
var rawUserId = context.RequestHeaders.GetValue("user_id");
if (!Guid.TryParse(rawUserId, out var userId))
    throw new RpcException(StatusCode.Unauthenticated, "Valid user_id header is required");
return userId;
```

## Связанные артефакты

* gRPC: `#grpc-ListReaderLibraryBooks`, `#grpc-SaveReaderLibraryBook`, … (все library RPC)
* КАР-3: [[../../02 - Архитектура/03 - КАР-3 - gRPC-only API и контекст user_id]]
* Интеграция: [[../Интеграции со сторонними сервисами/02 - Aggregator Service (gRPC caller)]]

---

# Алгоритм Health liveness (/healthz)

## Контекст и область применения

### Бизнес-требование

SR-MEDIA-OPS-01

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Docker Compose healthcheck |
| 2 | CI `docker compose ps` |
| 3 | Deploy workflow liveness probe |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Не проверяет MinIO — только «процесс Kestrel жив» |
| 2 | Readiness S3 — отдельная concern (не реализована) |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| HTTP GET | — | `/healthz` | Да |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| JSON body | object | `{ "status": "ok" }` |
| HTTP status | int | 200 |

## Логика работы (Псевдокод)

```csharp
// Program.cs
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
// Kestrel :5121, HttpProtocols.Http2 (gRPC + minimal HTTP)
```

## Связанные артефакты

* SR-MEDIA-OPS-01: [[../../01 - Функциональная спецификация/Возможности сервиса/04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-01]]
* gRPC ops doc: [[../Методы API/gRPC/04 - Платформенные контракты (Operations)]]
