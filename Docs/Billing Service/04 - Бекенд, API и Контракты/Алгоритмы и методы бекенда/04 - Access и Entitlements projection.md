# Введение

Группа алгоритмов **Access и Entitlements projection** определяет **effective SaaS plan** пользователя и выдаёт snapshot access / map лимитов для UI и VocabularyService.

**SR:** **SR-BILL-ACCESS-01**, **SR-BILL-ENT-01**. КАР: [[02 - КАР-4 - Free plan fallback|КАР-4]]. gRPC: `#grpc-CheckAccess`, `#grpc-GetEntitlements`.

---

# 1. Список алгоритмов

| Название алгоритма | SR | Краткое описание |
| :--- | :--- | :--- |
| **FindEffectiveSubscription** | SR-BILL-ACCESS-01, SR-BILL-ENT-01 | Active/Trialing/PastDue grace selection |
| **Free plan fallback** | SR-BILL-ACCESS-01, SR-BILL-ENT-01 | Default plan когда нет paid effective row |

---

# Алгоритм FindEffectiveSubscription

## Контекст и область применения

### Почему был создан

У customer может быть история subscriptions; access и entitlements должны отражать **одну** effective paid row с учётом grace period после failed renewal.

### Бизнес-требование

**SR-BILL-ACCESS-01**, **SR-BILL-ENT-01** — единая логика для CheckAccess и GetEntitlements.

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | `#grpc-CheckAccess` — UI badge «Pro до …» |
| 2 | `#grpc-GetEntitlements` — Vocabulary limit enforcement |
| 3 | `#grpc-GetSubscription` — effective snapshot (spec) |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | `Incomplete` / `Canceled` / `Unpaid` вне grace — не effective |
| 2 | При нескольких кандидатах — max `CurrentPeriodEnd` |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `subscriptions` | `IEnumerable<BillingSubscription>` | Все подписки customer | Да |
| `now` | `DateTime` | UTC timestamp | Да |
| `gracePeriodDays` | `int` | Из `Billing:GracePeriodDays` | Да |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `BillingSubscription?` | entity | Effective row или null |

## Логика работы (Псевдокод)

```csharp
var graceCutoff = now.AddDays(-gracePeriodDays);

return subscriptions
    .Where(s =>
        (s.Status is Active or Trialing && s.CurrentPeriodEnd > now)
        || (s.Status == PastDue && s.CurrentPeriodEnd >= graceCutoff))
    .OrderByDescending(s => s.CurrentPeriodEnd)
    .FirstOrDefault();
```

## Связанные артефакты

* gRPC: `#grpc-CheckAccess`, `#grpc-GetEntitlements`, `#grpc-GetSubscription`
* Entity: [[Entity - Подписки SaaS - Subscriptions#5. Effective subscription selection|Effective subscription selection]]

---

# Алгоритм Free plan fallback

## Контекст и область применения

### Почему был создан

Пользователи без paid subscription должны получать базовые лимиты платформы без ошибок — monetization через upgrade, не hard block.

### Бизнес-требование

**SR-BILL-ACCESS-01** — `has_access=true` на free; **SR-BILL-ENT-01** — entitlements default plan.

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | New user без checkout |
| 2 | Canceled subscription после period end |
| 3 | Vocabulary fail-open при Billing down (caller-side NFR) |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Требуется seed plan с `IsDefault=true` (`free`) |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| effectiveSubscription | `BillingSubscription?` | null если нет paid row | Да |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `plan_code` | `string` | `free` или effective plan code |
| `has_access` | `bool` | true в v1 |
| `entitlements` | map | Из default plan jsonb |

## Логика работы (Псевдокод)

```csharp
if (effectiveSubscription != null)
    return MapFromPlan(effectiveSubscription.Plan);

var defaultPlan = await Plans.FirstOrDefault(p => p.IsDefault);
return new AccessCheckResult(
    hasAccess: true,
    planCode: defaultPlan?.Code ?? "free",
    status: "active",
    periodEnd: null);
```

**Seed v1 free entitlements:** `maxProjects=3`, `maxCards=500`, `aiRequestsPerDay=10`.

## Связанные артефакты

* gRPC: `#grpc-CheckAccess`, `#grpc-GetEntitlements`
* КАР: [[02 - КАР-4 - Free plan fallback]]
* Entity: [[Entity - Каталог SaaS-планов - Plans|plans]]
