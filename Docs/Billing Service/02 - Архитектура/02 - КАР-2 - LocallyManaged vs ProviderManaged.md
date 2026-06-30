# Введение

Подписки Billing Service поддерживают два режима **`ManagementMode`**: провайдер управляет renewal (webhooks) или платформа продлевает локально (recurring API).

## Контекст и проблема

ЮKassa не предоставляет native subscription object как Stripe Billing. Без явного режима код мог бы ошибочно запускать RenewalWorker для Stripe или игнорировать renewal для ЮKassa.

## Принятое решение

1. Поле `subscriptions.ManagementMode`: `ProviderManaged` | `LocallyManaged`.
2. **ЮKassa checkout** создаёт `LocallyManaged` subscription.
3. **RenewalWorker** фильтрует только `LocallyManaged` + not `CancelAtPeriodEnd`.
4. **ProviderManaged** (future): sync через `SubscriptionUpdatedEvent` webhooks; worker disabled for row.

## Обоснование и последствия

### Плюсы

* Явная модель в БД — ops видит кто отвечает за renewal.
* Worker scope ограничен — меньше surprise charges.

### Последствия

* LocallyManaged требует saved `payment_methods` — checkout must save PM.
* Grace period logic shared между access-check и worker cutoff.

SR: [[01 - Функциональная спецификация/Возможности сервиса/08 - Автопродление (Renewal)#SR-BILL-REN-01|SR-BILL-REN-01]].

Entity: [[03 - Модель Данных/01 - Основные сущности/Entity - Подписки SaaS - Subscriptions]].
