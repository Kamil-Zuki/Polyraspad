# ISSUE-003: ProcessWebhook не проверяет подпись провайдера

## Тип

REST↔gRPC

## В двух словах

**SR-BILL-WH-01** требует verify signature перед apply events, но `BillingGrpcService.ProcessWebhook` сразу вызывает `HandleWebhookAsync` — `IPaymentProvider.VerifyWebhookSignature` не используется на gRPC entrypoint.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-BILL-WH-01 «Process webhook» | «Verify signature» в принципах |
| 04 | `rpc ProcessWebhook` | Документирован вызов verify, но в handler отсутствует |
| код | `BillingGrpcService.ProcessWebhook` | Нет вызова `VerifyWebhookSignature` |

Путь к файлу (вторично): `BillingService/Grpc/BillingGrpcService.cs`

## Доказательство

`YooKassaPaymentProvider.VerifyWebhookSignature` реализован, но `ProcessWebhook` в gRPC-слое переходит к `HandleWebhookAsync` после idempotency check без verify.

## Рекомендуемое действие

Добавить verify в `BillingGrpcService` (или orchestrator) до parse/apply; при failure — `PERMISSION_DENIED`. Либо ослабить SR-BILL-WH-01, если verify только на Aggregator.

## Статус

Open
