# Введение

Группа **Customers** обеспечивает запись billing customer для пары `(user_id, email)` перед checkout и recurring payments. Один пользователь Polyraspad → один `Customer` (unique `UserId` в v1).

**SR группы:** **SR-BILL-CUST-01**. Сущности: [[Entity - Клиенты и платёжные методы - Customers]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-BILL-CUST-01 | `EnsureCustomer` | Unary | Create-or-get customer по `user_id` + email. |

---

<span id="grpc-EnsureCustomer"></span>

# SR-BILL-CUST-01: Ensure customer: EnsureCustomer

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/01 - Управление клиентами (Customers)#SR-BILL-CUST-01]]

**REST-паритет:** `POST /api/Billing/customer/ensure` (Aggregator, JWT → `user_id`, email из claims).

| Сигнатура | `rpc EnsureCustomer(EnsureCustomerRequest) returns (EnsureCustomerResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `EnsureCustomerRequest` — `user_id` (string UUID), `email` (string) |
| **Сообщение ответа** | `EnsureCustomerResponse` — `customer_id` (string), `provider` (string, lowercase) |

## Логика обработки запроса

1. Распарсить `user_id` как Guid; при ошибке — `INVALID_ARGUMENT`.
2. `SubscriptionService.EnsureCustomerAsync(userId, email)`:
   - SELECT `customers` BY `UserId`;
   - если найден — обновить `Email` при изменении; вернуть существующую строку;
   - если нет — INSERT с `Provider = Billing:DefaultProvider`.
3. Смапить `customer.Id` → string `customer_id`; `Provider` → lowercase string.
4. Вернуть `EnsureCustomerResponse`.

Provider-side customer у payment adapter создаётся позже в `CreateCheckout`, не в этом RPC.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный формат `user_id`. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

*Следующая группа: [[02 - Каталог SaaS-планов (Plans)]].*
