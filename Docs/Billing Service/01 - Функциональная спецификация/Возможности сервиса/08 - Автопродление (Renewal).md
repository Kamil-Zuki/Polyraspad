# Группа 8: Автопродление (Renewal)

## Введение

В этом разделе описывается **RenewalWorker** — фоновое продление подписок в режиме `LocallyManaged` (ЮKassa v1).

Provider-managed подписки (future Stripe) **не** используют worker — renewal приходит только через webhooks.

**Метафора:**

Представьте **автоплатёж по расписанию в банке**. Раз в месяц система сама списывает абонемент с сохранённой карты; если карта недоступна — grace period, затем отключение.

Архитектура: [[02 - КАР-2 - LocallyManaged vs ProviderManaged]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к автопродлению подписок.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-BILL-REN-01** | **Renewal worker:** Poll interval, recurring charge, PastDue grace cutoff, skip при cancel_at_period_end. |

---

# Детальная спецификация требований

## SR-BILL-REN-01: Renewal worker {#SR-BILL-REN-01}

Background service продлевает LocallyManaged подписки до истечения `current_period_end`, используя default payment method customer.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **IHostedService** | `RenewalPollIntervalMinutes` из config (default 15). |
| **Scope per batch** | Новый DI scope на каждый poll. |
| **Renewal window** | `CurrentPeriodEnd <= now + 1 hour` для proactive charge. |
| **Default payment method** | Required; skip + log если нет. |
| **Grace cutoff** | PastDue после grace → Canceled. |
| **CancelAtPeriodEnd** | Worker не продлевает такие подписки. |

### 2. Высокоуровневое описание

Представим renewal worker как **автоплатёж по расписанию в банке**.

1. **Poll:** `IHostedService` каждые `RenewalPollIntervalMinutes` находит `LocallyManaged` subscriptions с `CurrentPeriodEnd <= now + 1 hour`.
2. **Skip rules:** worker пропускает `CancelAtPeriodEnd`, Canceled и подписки без default payment method.
3. **Recurring charge:** `CreateRecurringPaymentAsync` с saved `payment_method_id` customer.
4. **Outcome:** success → Active + extend period; fail → `PastDue`; после grace cutoff → `Canceled`.
5. **Webhook sync:** inbound webhook может дополнительно подтвердить payment — idempotency защищает от double-apply.

Таким образом, для ЮKassa renewal — **наша** ответственность; worker и webhook дополняют каждый друг, не дублируя state без idempotency keys.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Successful auto-renewal (Happy Path)

1. **Worker:** subscription Active, period ends in 30 min, default PM exists.
2. **Provider:** recurring payment succeeded.
3. **DB:** period +1 month, status Active.

#### Сценарий Б: No payment method (Negative Path)

1. **Worker:** log warning, subscription unchanged until period end → PastDue path.

---

*Следующая группа: [[09 - Платформенные контракты (Operations)]].*
