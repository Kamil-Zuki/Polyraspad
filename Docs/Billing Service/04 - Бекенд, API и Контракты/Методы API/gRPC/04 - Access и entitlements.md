# Введение

Методы данной группы реализуют **проверку доступа** и **выдачу лимитов** SaaS-тарифа — core value Billing Service для платформы.

`VocabularyService` вызывает `GetEntitlements` перед созданием проекта/карточки и AI-запросами. UI использует `CheckAccess` для badge и gating.

**SR группы:** **SR-BILL-ACCESS-01**, **SR-BILL-ENT-01**. Алгоритмы: [[../Алгоритмы и методы бекенда/04 - Access и Entitlements projection|Access и Entitlements projection]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-BILL-ACCESS-01 | `CheckAccess` | Unary | has_access, plan_code, status, period_end с grace для PastDue. |
| SR-BILL-ENT-01 | `GetEntitlements` | Unary | Map лимитов effective плана; fallback на default free plan. |

---

<span id="grpc-CheckAccess"></span>

# SR-BILL-ACCESS-01: Check access: CheckAccess

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/04 - Access и entitlements#SR-BILL-ACCESS-01]]

Лёгкий RPC для UI: «пользователь на paid плане или на free?» В v1 `has_access` почти всегда `true`; жёсткий deny — через entitlements enforcement в VocabularyService.

**REST-паритет:** `GET /api/Billing/access`.

| Сигнатура | `rpc CheckAccess(CheckAccessRequest) returns (CheckAccessResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `CheckAccessRequest` — `user_id` |
| **Сообщение ответа** | `CheckAccessResponse` — `has_access`, `plan_code`, `status`, optional `current_period_end` |

## Логика обработки запроса

1. Распарсить `user_id`.
2. Загрузить customer с subscriptions + plans (AsNoTracking).
3. Вызвать `SubscriptionQueryHelper.FindEffectiveSubscription(subscriptions, now, GracePeriodDays)`.
4. **Если effective subscription найдена:** вернуть `has_access=true`, `plan_code` из plan, `status` lowercase, `current_period_end` из subscription.
5. **Free fallback** ([[02 - КАР-4 - Free plan fallback|КАР-4]]): загрузить plan с `IsDefault=true`; вернуть `has_access=true`, `plan_code=free` (или default code), `status=active`, `current_period_end` пустой.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный `user_id`. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

<span id="grpc-GetEntitlements"></span>

# SR-BILL-ENT-01: Get entitlements: GetEntitlements

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/04 - Access и entitlements#SR-BILL-ENT-01]]

Downstream enforcement: numeric limits из jsonb плана (`maxProjects`, `maxCards`, `aiRequestsPerDay`). VocabularyService при недоступности Billing — fail-open на free (NFR-BILL-005).

**Вызывающие:** `VocabularyService` (gRPC direct), Aggregator `GET /api/Billing/entitlements`.

| Сигнатура | `rpc GetEntitlements(GetEntitlementsRequest) returns (GetEntitlementsResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `GetEntitlementsRequest` — `user_id` |
| **Сообщение ответа** | `GetEntitlementsResponse` — `plan_code`, `map<string,string> entitlements` |

## Логика обработки запроса

1. Распарсить `user_id`.
2. Применить ту же effective-subscription логику, что и `CheckAccess` (`SubscriptionQueryHelper`).
3. Если effective plan найден — взять `Plan.Entitlements` jsonb.
4. Иначе — загрузить default plan (`IsDefault=true`) или первый plan в БД.
5. Скопировать entitlements в proto map (case-insensitive keys).
6. Вернуть `plan_code` и map лимитов.

**Seed v1 (free):** `maxProjects=3`, `maxCards=500`, `aiRequestsPerDay=10`. **Pro:** `50`, `10000`, `100`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный `user_id`. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

*Следующая группа: [[05 - Инвойсы (Invoices)]].*
