# Введение

Vocabulary Service — **внутренний gRPC-микросервис**. Публичный REST и JWT — зона **Aggregator Service** (BFF). Браузер не вызывает Vocabulary напрямую.

## Контекст и проблема

Если отдать доменный REST наружу из Vocabulary:

* дублируется auth/CORS/rate-limit с Aggregator;
* усложняется эволюция контрактов (REST + gRPC одновременно);
* нарушается единый периметр Polyraspad (`api.polyraspad.online`).

## Принятое решение

1. **Aggregator** принимает HTTP/JWT, мапит DTO → gRPC Vocabulary (Content, Card, Term, Text, Study, Analytics, Community, Subscription).
2. **Vocabulary** слушает gRPC (h2c в Docker), identity — из metadata (`user_id`), не из Cookie.
3. **Agent Service** — второй gRPC-клиент для project-scoped tools.
4. Тяжёлая бизнес-логика (FSRS queue, term statuses, marketplace) остаётся в Vocabulary, не в BFF.

## Обоснование и последствия

* Thin BFF: Aggregator не владеет vocabulary DB.
* Единый внутренний контракт для UI и Agent.
* Смена публичного REST не требует переписывать доменный слой.
