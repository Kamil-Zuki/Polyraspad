# ISSUE-004: GetSubscription не использует effective subscription logic

## Тип

Противоречие

## В двух словах

`01` SR-BILL-SUB-01 требует «effective row» с grace для PastDue. `SubscriptionService.GetActiveSubscriptionAsync` возвращает только Active/Trialing с `CurrentPeriodEnd > now`, без `SubscriptionQueryHelper` и без PastDue в grace window.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-BILL-SUB-01 | «Effective row» — последняя релевантная подписка |
| 03 | Effective subscription selection | PastDue + grace cutoff |
| 04 | `#grpc-GetSubscription` | Шаг 3: `FindEffectiveSubscription` |
| Код | `GetActiveSubscriptionAsync` | Filter только Active/Trialing |

Путь к файлу (вторично): `BillingService/Services/SubscriptionService.cs`

## Доказательство

Код: `.Where(s => s.Status == Active || s.Status == Trialing).Where(s => s.CurrentPeriodEnd > now)`

`CheckAccess` / `GetEntitlements` используют `SubscriptionQueryHelper.FindEffectiveSubscription` с grace.

## Рекомендуемое действие

Унифицировать `GetSubscription` через `SubscriptionQueryHelper` (как в `04` gRPC doc) или явно уточнить SR-BILL-SUB-01 если UI snapshot намеренно уже.

## Статус

Open
