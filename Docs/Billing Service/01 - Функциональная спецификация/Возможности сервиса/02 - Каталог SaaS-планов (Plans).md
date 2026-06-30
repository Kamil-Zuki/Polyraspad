# Группа 2: Каталог SaaS-планов (Plans)

## Введение

В этом разделе описывается **provider-agnostic каталог тарифов** платформы — планы `free` и `pro` с ценой, trial и entitlements.

Каталог хранится в PostgreSQL (`plans`); изменение лимитов — через seed/migration или админ-процесс (v1 — seed only). Публичный UI читает планы через Aggregator `GET /api/billing/plans` → gRPC `ListPlans`.

**Метафора:**

Представьте **витрину тарифов в мобильном операторе**. На стенке (ListPlans) — названия, цены и «пакеты минут» (entitlements). Реальная оплата подключается отдельно (checkout); витрина не знает, картой или СБП платит клиент.

Сущности: [[Entity - Каталог SaaS-планов - Plans]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к каталогу планов.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-PLAN-01** | **List plans:** Возврат активных SaaS-планов с price, currency, interval, trial_days и entitlements map для billing UI. |

---

# Детальная спецификация требований

## SR-BILL-PLAN-01: List plans {#SR-BILL-PLAN-01}

Billing page и upgrade modal должны показывать актуальные тарифы без hardcode во frontend.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Provider-agnostic** | План не содержит YooKassa-specific fields в gRPC `Plan` message. |
| **only_active filter** | `ListPlansRequest.only_active` — скрыть неактивные планы из UI. |
| **Entitlements as map** | `maxProjects`, `maxCards`, `aiRequestsPerDay` — строки в proto map. |
| **Default plan** | `free` с `IsDefault=true` — не продается через checkout, но всегда в каталоге. |

### 2. Высокоуровневое описание

Представим каталог планов как **витрину тарифов в салоне связи**.

1. **Запрос UI:** billing page или upgrade modal вызывает `GET /api/billing/plans` → gRPC `ListPlans(only_active=true)`.
2. **Чтение каталога:** Billing возвращает активные строки `plans` с price, currency, interval, trial_days и entitlements map.
3. **Отрисовка:** frontend сравнивает лимиты Free vs Pro и показывает CTA Upgrade на `pro` без hardcode тарифов в коде.

Seed v1: `free` (0 ₽), `pro` (990 ₽/month, 7-day trial).

Таким образом, `ListPlans` — единственный источник истины для публичной витрины SaaS-тарифов; checkout подключается отдельно.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Billing page load (Happy Path)

1. **REST:** GET `/api/billing/plans` (Aggregator).
2. **gRPC:** `ListPlans(only_active=true)`.
3. **Ответ:** массив `Plan` с entitlements для сравнения лимитов.

---

*Следующая группа: [[03 - Подписки SaaS (Subscriptions)]].*
