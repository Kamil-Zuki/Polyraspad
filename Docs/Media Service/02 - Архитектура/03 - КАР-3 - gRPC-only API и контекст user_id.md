# Введение

Media Service экспонирует **только gRPC** (`media.proto`); аутентификация пользователя делегирована **Aggregator**, который прокидывает **`user_id`** в metadata.

## Контекст и проблема

Library данные изолированы по owner userId в S3 paths. Сервис не должен парсить JWT (разные ключи, clock skew) — единая точка auth на perimeter.

## Принятое решение

1. Kestrel **Http2** на 5121 для gRPC.
2. Library RPC: `GetRequiredUserId` из `context.RequestHeaders["user_id"]`.
3. Invalid/missing → `RpcException(Unauthenticated)`.
4. Upload RPC: без user_id в текущей реализации (public-ish blobs; access via URL/proxy).
5. Max message size 1 GB для large document upload.

## Обоснование и последствия

### Плюсы

* Thin Media Service — нет JwtBearer dependency.
* Consistent с другими internal gRPC services (Vocabulary metadata pattern).

### Последствия

* **Forged user_id** возможен при компрометации internal network — trust boundary = Docker network + only BFF calls.
* *Решение:* mTLS / service mesh в production hardening (out of v1 scope).

*Связанные SR:* [[04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-02|SR-MEDIA-OPS-02]].
