# ISSUE-002: ProcessWebhook использует hash payload вместо EventId провайдера

## Тип

Противоречие

## В двух словах

В `03` поле `processed_webhooks.EventId` — уникальный ID события **от провайдера**. В коде `BillingGrpcService.ProcessWebhook` в качестве `EventId` записывается SHA-256 hex всего payload, что не совпадает с моделью и мешает корректной dedup при разных payload одного payment event.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 03 | `processed_webhooks.EventId` | «Уникальный ID события от провайдера» |
| 04 | `#grpc-ProcessWebhook` | Шаг 3: извлечь EventId из payload провайдера |
| Код | `BillingGrpcService.ProcessWebhook` | `ComputePayloadHash(request.Payload)` → EventId |

Путь к файлу (вторично): `BillingService/Grpc/BillingGrpcService.cs`

## Доказательство

`03`: «`EventId` | text | PK (part) | Уникальный ID события от провайдера.»

Код: `var eventId = ComputePayloadHash(request.Payload);`

## Рекомендуемое действие

Парсить provider event id из JSON (YooKassa: `object.id` + `event`) в adapter; хранить `PayloadHash` отдельно. Обновить код или согласовать изменение `03` явным запросом пользователя.

## Статус

Open
