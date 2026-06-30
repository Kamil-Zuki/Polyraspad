# ISSUE-003: ProcessWebhook не вызывает VerifyWebhookSignature

## Тип

Пробел

## В двух словах

SR-BILL-WH-01 и `03` требуют verify webhook signature перед apply. `BillingGrpcService.ProcessWebhook` сразу проверяет idempotency и вызывает `HandleWebhookAsync`, не вызывая `IPaymentProvider.VerifyWebhookSignature`.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-BILL-WH-01 | «Verify signature, idempotency insert, provider parse» |
| 03 | `processed_webhooks` алгоритм шаг 1 | `VerifyWebhookSignature` |
| 04 | `#grpc-ProcessWebhook` | Шаг 2: verify signature → PERMISSION_DENIED |
| Код | `BillingGrpcService.ProcessWebhook` | Нет вызова `VerifyWebhookSignature` |

Путь к файлу (вторично): `BillingService/Grpc/BillingGrpcService.cs`

## Доказательство

`IPaymentProvider` defines `bool VerifyWebhookSignature(WebhookPayload payload, string? secret)` — метод существует в `YooKassaPaymentProvider`, но не вызывается из gRPC handler.

## Рекомендуемое действие

Добавить verify до idempotency check в `ProcessWebhook`; при failure — `RpcException(PermissionDenied)`.

## Статус

Open
