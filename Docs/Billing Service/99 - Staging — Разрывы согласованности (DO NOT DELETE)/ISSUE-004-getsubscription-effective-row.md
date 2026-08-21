# ISSUE-004: GetSubscription ≠ FindEffective (access / entitlements)

## Тип

Противоречие

## В двух словах

`01` SR-BILL-SUB-01 выровнен с кодом: только Active/Trialing + period > now. CheckAccess / GetEntitlements используют `FindEffectiveSubscription` (включая PastDue + grace). UI snapshot и access-check могут расходиться. Ранее дублировался как ISSUE-002-getsubscription-effective-logic — канон теперь ISSUE-004.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-BILL-SUB-01 | Active/Trialing only (code-aligned) |
| 03 | FindEffective (access) | PastDue + GracePeriodDays |
| код | `GetActiveSubscriptionAsync` vs `SubscriptionQueryHelper` | Разные селекторы |

Путь (вторично): `BillingService/Services/SubscriptionService.cs`

## Доказательство

GetSubscription filter: `Active || Trialing` и `CurrentPeriodEnd > now`. CheckAccess: `FindEffectiveSubscription`.

## Рекомендуемое действие

Унифицировать GetSubscription через helper **или** оставить намеренный узкий UI snapshot и держать этот ISSUE как известный product gap.

## Статус

Open
