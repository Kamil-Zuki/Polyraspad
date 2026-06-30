# ISSUE-002: GetSubscription уже не совпадает с CheckAccess

## Тип

REST↔gRPC

## В двух словах

В `01` **SR-BILL-SUB-01** описывает «effective subscription» с grace для PastDue, но RPC `GetSubscription` в коде возвращает только `Active`/`Trialing` с `CurrentPeriodEnd > now`, без `SubscriptionQueryHelper`. `CheckAccess` и `GetEntitlements` используют helper с grace — snapshot подписки для UI может расходиться с access-check.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-BILL-SUB-01 «Get subscription» | «последняя релевантная подписка», сценарий PastDue/grace |
| 04 | `rpc GetSubscription` | Фильтр только Active/Trialing, period в будущем |
| код | `SubscriptionService.GetActiveSubscriptionAsync` | Не вызывает `SubscriptionQueryHelper` |

Путь к файлу (вторично): `04/…/03 - Подписки SaaS (Subscriptions).md`

## Доказательство

`GetActiveSubscriptionAsync`:

```csharp
.Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
.Where(s => s.CurrentPeriodEnd > now)
```

`CheckAccess` / `GetEntitlements` — `SubscriptionQueryHelper.FindEffectiveSubscription` с `PastDue` + `GracePeriodDays`.

## Рекомендуемое действие

Либо унифицировать `GetSubscription` с `SubscriptionQueryHelper`, либо уточнить SR-BILL-SUB-01, что UI snapshot намеренно уже access-projection.

## Статус

Open
