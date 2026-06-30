# Введение

Группа **Каталог SaaS-планов** описывает provider-agnostic каталог тарифов платформы Polyraspad и маппинг цен на внешние product/price ID провайдеров.

Планы определяют **цену**, **интервал** и **entitlements** (лимиты `maxProjects`, `maxCards`, `aiRequestsPerDay`), которые читают `VocabularyService` через gRPC `GetEntitlements`.

---

# SaaS-план (`plans`)

## 1. Общее описание

**SaaSPlan** (`plans`) — каталог тарифов. Не зависит от ЮKassa/Stripe: доменная логика access-check читает `Code` и `Entitlements` jsonb.

Обязательный план **`free`** (`IsDefault = true`, `Price = 0`) — fallback когда нет активной paid-подписки.

## 2. Атрибуты (поля) сущности

**Таблица:** `billing.plans`

| Название | Тип данных | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `Id` | `uuid` | PK | Внутренний ID плана. |
| `Code` | `text` | NOT NULL, UNIQUE | Стабильный код (`free`, `pro`). |
| `Name` | `text` | NOT NULL | Отображаемое имя (UI billing page). |
| `Description` | `text` | NOT NULL | Описание для каталога. |
| `Price` | `int` | NOT NULL | Цена в минимальных единицах (копейки для RUB). |
| `Currency` | `text` | NOT NULL | Валюта (`RUB` в v1). |
| `Interval` | `text` | NOT NULL | `month` или `year`. |
| `IsActive` | `boolean` | NOT NULL | Доступен для новых checkout. |
| `IsDefault` | `boolean` | NOT NULL | Fallback план (один `free`). |
| `TrialDays` | `int` | NOT NULL | Длина trial для paid планов (0 для free). |
| `Entitlements` | `jsonb` | NOT NULL | Map лимитов платформы (строковые значения). |

*Индексы:* UNIQUE (`Code`).

## 3. Связи

| Сущность | Описание |
| :--- | :--- |
| `subscriptions` | Многие подписки ссылаются на один план. |
| `plan_provider_prices` | Маппинг на внешние price IDs. |
| **VocabularyService** | Читает entitlements через gRPC, не таблицу напрямую. |

## 4. Seed (v1)

| Code | Price | TrialDays | Entitlements |
| :--- | :--- | :--- | :--- |
| `free` | 0 | 0 | maxProjects=3, maxCards=500, aiRequestsPerDay=10 |
| `pro` | 99000 | 7 | maxProjects=50, maxCards=10000, aiRequestsPerDay=100 |

---

# Цена плана у провайдера (`plan_provider_prices`)

## 1. Общее описание

**PlanProviderPrice** — связка внутреннего плана с product/price ID конкретного провайдера. Позволяет добавить Stripe без изменения доменной модели плана.

## 2. Атрибуты (поля) сущности

**Таблица:** `billing.plan_provider_prices`

| Название | Тип данных | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `Id` | `uuid` | PK | Внутренний ID. |
| `PlanId` | `uuid` | FK, NOT NULL | Ссылка на `plans.Id`. |
| `Provider` | `enum` | NOT NULL | `Mock`, `YooKassa`, `Stripe`. |
| `ProviderProductId` | `text` | NOT NULL | Product ID в провайдере. |
| `ProviderPriceId` | `text` | NOT NULL | Price ID в провайдере. |

*Индексы:* UNIQUE (`PlanId`, `Provider`).

## 3. Использование в v1

Таблица подготовлена для multi-provider catalog; checkout ЮKassa в v1 может использовать `Plan.Price` напрямую через API провайдера без заполнения всех price rows.
