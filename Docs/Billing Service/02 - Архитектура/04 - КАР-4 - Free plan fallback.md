# Введение

Пользователь **без активной paid-подписки** всегда получает access и entitlements **default плана** (`free` с `IsDefault=true`), а не hard deny.

## Контекст и проблема

Новые пользователи не имеют subscription row. Без fallback downstream сервисы не знают лимиты или ошибочно блокируют все действия.

## Принятое решение

1. `AccessService`: если нет effective subscription → `plan_code` from `IsDefault` plan, `has_access=true`.
2. `EntitlementService`: entitlements jsonb из default plan.
3. Seed migration создаёт `free` с `IsDefault=true` и базовые лимиты.
4. `SubscriptionQueryHelper` учитывает grace для `PastDue` paid plans.

## Обоснование и последствия

### Плюсы

* Predictable UX — free tier всегда работает.
* Vocabulary может enforce limits без special «no customer» case.

### Последствия

* Product must keep `free` plan in DB; deleting default breaks fallback.
* «has_access» не equals «paid» — UI must read `plan_code`.

SR: [[01 - Функциональная спецификация/Возможности сервиса/04 - Access и entitlements]].
