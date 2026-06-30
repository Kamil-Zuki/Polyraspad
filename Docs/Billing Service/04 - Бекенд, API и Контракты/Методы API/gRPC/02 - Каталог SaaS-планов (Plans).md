# Введение

Группа **Plans** — read-only каталог SaaS-тарифов из PostgreSQL (`SaaSPlan`). Entitlements хранятся как jsonb/map в proto `Plan.entitlements`.

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-BILL-PLAN-01 | `ListPlans` | Unary | Список планов с optional active filter |

---

<span id="grpc-ListPlans"></span>

# SR-BILL-PLAN-01: List plans: ListPlans

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/02 - Каталог SaaS-планов (Plans)#SR-BILL-PLAN-01]]

| Сигнатура | `rpc ListPlans(ListPlansRequest) returns (ListPlansResponse)` |
| :--- | :--- |
| **Запрос** | `only_active` (bool) — если true, фильтр `IsActive` |
| **Ответ** | `plans[]` — `Plan` message (code, price, currency, interval, trial_days, entitlements map) |

## Логика обработки запроса

1. Query `Plans` AsNoTracking; optional `Where(p => p.IsActive)`.
2. OrderBy `Price` ascending.
3. Map entity → proto `Plan` (entitlements key/value).

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INTERNAL** | DB failure |
