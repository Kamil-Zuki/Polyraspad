# Введение

Группа сущностей **Клиенты и платёжные методы** связывает глобального пользователя Polyraspad (`user_id` из authorization-module) с записью биллинга и токенизированными платёжными средствами провайдера.

Один пользователь платформы → один `Customer` (unique `UserId`). Платёжные карты и рекуррентные токены хранятся как **маски и provider IDs**, не как PAN.

---

# Клиент биллинга (`customers`)

## 1. Общее описание

**Customer** — локальная проекция пользователя SaaS-биллинга. Создаётся при первом checkout или явном `EnsureCustomer`. Хранит email для чеков и связку с платёжным провайдером (`Provider`, `ProviderCustomerId`).

**Архитектурное назначение:**

1. **Стабильный billing subject** — все подписки и инвойсы ссылаются на `Customer`, не на сырой `user_id` в каждой таблице.
2. **Provider binding (v1)** — один активный `Provider` на customer; смена провайдера = новая customer record (documented limitation).
3. **Soft delete column** — `DeletedAt` зарезервирован под GDPR/offboarding; **global query filter / RPC soft-delete filter в коде не реализован** (`EnsureCustomer` ищет только по `UserId`).

## 2. Атрибуты (поля) сущности

**Таблица:** `billing.Customers`

| Название | Тип данных | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `Id` | `uuid` | PK, NOT NULL | Внутренний ID customer в Billing Service. |
| `UserId` | `uuid` | NOT NULL, UNIQUE | Логическая ссылка на пользователя authorization-module (`sub` JWT). |
| `Email` | `text` | NOT NULL | Email для чеков и checkout; может обновляться при checkout. |
| `Provider` | `enum` | NOT NULL | `Mock`, `YooKassa`, `Stripe` — активный платёжный провайдер customer. |
| `ProviderCustomerId` | `text` | NULL | ID customer в системе провайдера (merchant customer id). |
| `CreatedAt` | `timestamp` | NOT NULL | UTC создания записи. |
| `DeletedAt` | `timestamp` | NULL | Soft-delete marker; **не фильтруется** в текущих RPC (колонка есть, filter нет). |

*Индексы:* UNIQUE (`UserId`).

## 3. Связи

| Сущность | Тип связи |
| :--- | :--- |
| `Subscriptions` | Один-ко-многим — история и текущие SaaS-подписки. |
| `PaymentMethods` | Один-ко-многим — сохранённые методы оплаты для renewal. |
| **authorization-module** | Source of truth для identity; Billing только хранит `UserId`. |
| **AggregatorService** | REST BFF передаёт `user_id` из JWT в gRPC. |

## 4. Жизненный цикл

1. **EnsureCustomer** — upsert по `UserId` + email; возвращает `customer_id` и `provider`.
2. **CreateCheckout** — обновляет `Provider` и email перед созданием подписки `incomplete`.
3. **Webhook PaymentMethodSaved** — может дополнить `ProviderCustomerId` через связанные payment events.

---

# Платёжный метод (`payment_methods`)

## 1. Общее описание

**PaymentMethod** — токенизированное платёжное средство у провайдера (saved card для ЮKassa autopayment). Используется **RenewalWorker** для `LocallyManaged` подписок.

PCI: хранятся только `ProviderPaymentMethodId`, `Brand`, `Last4`, срок карты — не полный номер.

## 2. Атрибуты (поля) сущности

**Таблица:** `billing.PaymentMethods`

| Название | Тип данных | Огр-ния | Описание |
| :--- | :--- | :--- | :--- |
| `Id` | `uuid` | PK | Внутренний ID. |
| `CustomerId` | `uuid` | FK, NOT NULL | Ссылка на `customers.Id`. |
| `Provider` | `enum` | NOT NULL | Провайдер, выдавший токен. |
| `ProviderPaymentMethodId` | `text` | NOT NULL | ID payment method в провайдере. |
| `Type` | `text` | NOT NULL | Тип (например `bank_card`). |
| `Brand` | `text` | NULL | Бренд карты (Visa, Mastercard). |
| `Last4` | `text` | NULL | Последние 4 цифры. |
| `ExpMonth` | `int` | NULL | Месяц истечения. |
| `ExpYear` | `int` | NULL | Год истечения. |
| `IsDefault` | `boolean` | NOT NULL | Default для recurring charge. |
| `CreatedAt` | `timestamp` | NOT NULL | UTC создания. |

*Индексы:* UNIQUE composite (`Provider`, `ProviderPaymentMethodId`) — предотвращение дублей токена.

## 3. Связи и логика

| Процесс | Использование |
| :--- | :--- |
| Checkout с `save_payment_method` | ЮKassa сохраняет метод; webhook → `PaymentMethodSavedEvent`. |
| RenewalWorker | Выбирает `IsDefault = true` для `CreateRecurringPaymentAsync`. |
