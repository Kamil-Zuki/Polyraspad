# ISSUE-002: ProcessWebhook: EventId = SHA-256(payload), не provider event id

## Тип

Противоречие (код ↔ желаемый product)

## В двух словах

`03` теперь описывает фактическое поведение: `EventId` и `PayloadHash` = один и тот же SHA-256 hex payload. Желаемый product (provider-native event id + отдельный hash) в коде не реализован — dedup ломается при изменении payload одного и того же payment event.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 03 | `ProcessedWebhooks.EventId` / `PayloadHash` | Документировано как hash payload (code-aligned) |
| код | `BillingGrpcService.ProcessWebhook` | `ComputePayloadHash` → оба поля |
| product intent | Idempotency по event провайдера | Не реализовано |

Путь (вторично): `BillingService/Grpc/BillingGrpcService.cs`

## Доказательство

`var eventId = ComputePayloadHash(request.Payload);` затем `EventId = eventId`, `PayloadHash = eventId`.

## Рекомендуемое действие

Парсить provider event id в adapter; хранить hash отдельно. Пока код не изменён — `03` остаётся code-aligned.

## Статус

Open
