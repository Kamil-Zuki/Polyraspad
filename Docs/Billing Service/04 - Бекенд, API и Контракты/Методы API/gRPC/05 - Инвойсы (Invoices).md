# Введение

Методы данной группы предоставляют **историю платежей** пользователя — инвойсы, созданные из webhook events и renewal pipeline, для UI `/billing/invoices`.

Создание инвойсов — **не** через user API; только projection из `PaymentSucceeded` events.

**SR группы:** **SR-BILL-INV-01**. Сущности: [[Entity - Инвойсы и webhook-идемпотентность - Invoices#Инвойс (`invoices`)|invoices]].

---

# 1. Список методов

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-BILL-INV-01 | `ListInvoices` | Unary | Пагинированный список инвойсов customer. |

---

<span id="grpc-ListInvoices"></span>

# SR-BILL-INV-01: List invoices: ListInvoices

## Общая информация

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Инвойсы (Invoices)#SR-BILL-INV-01]]

Пользователь видит историю SaaS-платежей — суммы, статусы, даты для support correlation через `provider_invoice_id`.

**REST-паритет:** `GET /api/Billing/invoices?page=1&pageSize=20`.

| Сигнатура | `rpc ListInvoices(ListInvoicesRequest) returns (ListInvoicesResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ListInvoicesRequest` — `user_id`, `page` (default 1), `page_size` (1–100) |
| **Сообщение ответа** | `ListInvoicesResponse` — `repeated Invoice invoices` |

**Поля `Invoice` (proto):** `id`, `subscription_id`, `provider`, `provider_invoice_id`, `amount_due`, `amount_paid`, `currency`, `status`, optional `invoice_pdf_url`, `paid_at`, `created_at`.

## Логика обработки запроса

1. Распарсить `user_id`.
2. Нормализовать пагинацию: `page = max(1, page)`, `page_size = clamp(1..100)`.
3. Query `billing.invoices` JOIN subscription → customer WHERE `Customer.UserId = user_id`.
4. ORDER BY `CreatedAt` DESC; SKIP `(page-1)*page_size`; TAKE `page_size`.
5. Смапить каждую entity → proto `Invoice` (amounts в копейках, status lowercase).
6. Вернуть `ListInvoicesResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание |
| :--- | :--- |
| **INVALID_ARGUMENT** | Невалидный `user_id`. |
| **INTERNAL** | Ошибка PostgreSQL. |

---

*Следующая группа: [[06 - Webhook-оркестрация (Webhooks)]].*
