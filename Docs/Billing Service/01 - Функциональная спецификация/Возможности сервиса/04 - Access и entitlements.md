# Группа 4: Access и entitlements

## Введение

В этом разделе описывается **проверка доступа** и **выдача лимитов** SaaS-тарифа — core value Billing Service для остальной платформы.

`VocabularyService` вызывает `GetEntitlements` перед созданием проекта/карточки и AI-запросами. UI может вызывать `CheckAccess` для badge и gating.

**Метафора:**

Представьте **лимиты на корпоративном тарифном плане интернета**. Роутер (Vocabulary) перед «открыть новый проект» спрашивает биллинг: «сколько слотов осталось?» Access-check — «есть ли действующий абонемент?»; entitlements — «сколько слотов в пакете».

SR: [[04 - Access и entitlements#SR-BILL-ACCESS-01|SR-BILL-ACCESS-01]], [[04 - Access и entitlements#SR-BILL-ENT-01|SR-BILL-ENT-01]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к access и entitlements.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-ACCESS-01** | **Check access:** has_access, plan_code, status, current_period_end с grace для PastDue. |
| **SR-BILL-ENT-01** | **Get entitlements:** Map лимитов effective плана; fallback на default free plan. |

---

# Детальная спецификация требований

## SR-BILL-ACCESS-01: Check access {#SR-BILL-ACCESS-01}

Лёгкий RPC для UI: «пользователь на paid плане или на free?»

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Effective subscription** | `SubscriptionQueryHelper` + `GracePeriodDays`. |
| **Free fallback** | `has_access=true` даже на free; `plan_code=free`, `status=active`. |
| **PastDue grace** | Access сохраняется в grace window после failed renewal. |

### 2. Высокоуровневое описание

Представим check access как **проверку действующего абонемента на входе в зал**.

1. **Запрос UI:** Aggregator или frontend вызывает `CheckAccess(user_id)` для badge и gating.
2. **Effective subscription:** `AccessService` загружает подписки customer, применяет `GracePeriodDays` для `PastDue` и выбирает effective row.
3. **Free fallback:** если paid row нет — `has_access=true`, `plan_code=free`, `status=active`.
4. **Snapshot:** ответ содержит `plan_code`, `status`, `current_period_end` для отображения «Pro до …».

Таким образом, `CheckAccess` — лёгкий RPC «на каком тарифе пользователь»; в v1 `has_access` почти всегда true, жёсткий deny идёт через entitlements.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Pro active (Happy Path)

1. **gRPC:** `CheckAccess(user_id)`.
2. **Ответ:** `has_access=true`, `plan_code=pro`, `status=active`, `current_period_end` set.

#### Сценарий Б: PastDue inside grace (Happy Path)

1. **Subscription:** `PastDue`, period_end внутри grace.
2. **Ответ:** effective subscription treated as active for access snapshot.

---

## SR-BILL-ENT-01: Get entitlements {#SR-BILL-ENT-01}

Downstream enforcement: numeric limits из jsonb плана.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Plan entitlements jsonb** | Ключи: `maxProjects`, `maxCards`, `aiRequestsPerDay`. |
| **Same effective logic** | Как AccessService для выбора плана. |
| **Proto map** | `map<string,string>` в gRPC response. |
| **Vocabulary integration** | Fail-open на free при недоступности Billing (NFR-BILL-005). |

### 2. Высокоуровневое описание

Представим entitlements как **пакет минут на корпоративном тарифе интернета**.

1. **Downstream запрос:** Vocabulary перед create project/card или AI-вызовом вызывает `GetEntitlements(user_id)`.
2. **Выбор плана:** Billing применяет ту же effective-subscription логику, что `AccessService`, или fallback на default `free` plan.
3. **Map лимитов:** из jsonb плана возвращается proto map — `maxProjects`, `maxCards`, `aiRequestsPerDay`.
4. **Enforcement:** caller сравнивает текущий count с лимитом (Free: 3 projects; Pro: 50).

Таким образом, `GetEntitlements` — источник числовых лимитов SaaS-тарифа; при недоступности Billing Vocabulary fail-open на free (NFR-BILL-005).

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Free user entitlements (Happy Path)

1. **gRPC:** `GetEntitlements`.
2. **Ответ:** `plan_code=free`, map `maxProjects=3`, `maxCards=500`, `aiRequestsPerDay=10`.

---

*Следующая группа: [[05 - Инвойсы (Invoices)]].*
