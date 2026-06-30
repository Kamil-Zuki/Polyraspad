# Группа 1: Управление клиентами (Customers)

## Введение

В этом разделе описывается создание и обеспечение записи **billing customer** — связки `user_id` (из authorization-module) с email и платёжным провайдером.

Без customer checkout и recurring payments не могут стартовать. Один пользователь Polyraspad → один `Customer` (unique `UserId` в v1).

**Метафора:**

Представьте **клиентский кабинет в банке**. Прежде чем открыть счёт для автоплатежей, банк (Billing Service) заводит карточку клиента с внутренним номером. Глобальный паспорт (JWT `user_id`) — внешний идентификатор; кабинет биллинга — локальная карточка для подписок и инвойсов.

gRPC: `EnsureCustomer`. Сущности: [[Entity - Клиенты и платёжные методы - Customers]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к управлению customers.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-CUST-01** | **Ensure customer:** Upsert customer по `user_id` и email; возврат `customer_id` и активного `provider` для downstream checkout. |

---

# Детальная спецификация требований

## SR-BILL-CUST-01: Ensure customer {#SR-BILL-CUST-01}

Перед checkout или явной инициализацией billing UI система должна иметь строку в `customers`. RPC не требует JWT — caller (Aggregator) передаёт уже проверенный `user_id`.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **One customer per user** | UNIQUE `UserId`; повторный вызов обновляет email при необходимости. |
| **Provider binding** | `Provider` из `Billing:DefaultProvider` или уже сохранённый на customer. |
| **No auth in Billing** | Identity приходит из caller; Billing не валидирует JWT. |
| **Soft delete aware** | Записи с `DeletedAt` не используются для новых checkout (future). |

### 2. Высокоуровневое описание

Представим customer как **номер лицевого счёта в SaaS**.

1. **Запрос billing UI:** Frontend открывает `/billing` — Aggregator вызывает `EnsureCustomer(user_id, email)`.
2. **Upsert customer:** Billing находит или создаёт `Customer`, синхронизирует email для чеков.
3. **Стабильный якорь:** UI получает `customer_id` для статуса и последующих RPC.

Таким образом, customer — **якорь** для всех подписок и payment methods одного пользователя.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Caller:** AggregatorService `BillingController` (JWT → user_id).
* **RPC:** `EnsureCustomer`.

#### Сценарий А: Первый визит billing page (Happy Path)

1. **gRPC:** `EnsureCustomer(user_id, email)`.
2. **DB:** INSERT `customers` с `Provider = DefaultProvider`.
3. **Ответ:** `customer_id`, `provider`.

#### Сценарий Б: Повторный визит (Happy Path)

1. **DB:** SELECT by `UserId` — существующая строка.
2. **Ответ:** тот же `customer_id`; email обновлён если изменился в профиле.

---

*Следующая группа: [[02 - Каталог SaaS-планов (Plans)]].*
