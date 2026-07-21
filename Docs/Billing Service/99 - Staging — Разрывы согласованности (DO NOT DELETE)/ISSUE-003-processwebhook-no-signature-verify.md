# ISSUE-003: ProcessWebhook не вызывает VerifyWebhookSignature

## Тип

Пробел

## В двух словах

SR-BILL-WH-01 ожидает verify webhook signature перед apply. `BillingGrpcService.ProcessWebhook` не вызывает `IPaymentProvider.VerifyWebhookSignature`. Дубликат `ISSUE-003-processwebhook-signature-verify.md` сведён сюда как канон.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-BILL-WH-01 | «Verify signature…» |
| код | `BillingGrpcService.ProcessWebhook` | Нет вызова verify |
| 04 | `#grpc-ProcessWebhook` | Может всё ещё описывать verify step |

Путь (вторично): `BillingService/Grpc/BillingGrpcService.cs`

## Доказательство

`VerifyWebhookSignature` есть у провайдеров, но gRPC handler идёт в idempotency → `HandleWebhookAsync` без verify.

## Рекомендуемое действие

Добавить verify до apply; при failure — `PERMISSION_DENIED`. Или явно сузить SR, если verify только на Aggregator.

## Статус

Open
