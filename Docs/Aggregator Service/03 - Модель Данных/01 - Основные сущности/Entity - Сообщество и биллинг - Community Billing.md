# Entity - Сообщество и биллинг - Community Billing

**Тип:** API Contract View

Downstream: Vocabulary Community/Subscription + BillingService.

## Community (контракт)

| Объект | Описание |
| :--- | :--- |
| Contribution | Предложение правок; resolve PENDING→APPROVED/REJECTED |
| Product / Review | Marketplace listing и отзывы |
| AuthorProfile | Публичный профиль автора |
| Entitlement | Доступ к колоде |
| Publish / Fork | Публикация и fork колоды |
| DeckSubscription | List/Subscribe/Unsubscribe |

## Billing (контракт)

| Объект | Описание |
| :--- | :--- |
| Access / Entitlements | SaaS access check |
| Subscription / Plans | Текущая подписка и каталог планов |
| Checkout / Cancel | Self-service |
| Invoices | Список инвойсов |
| Webhook | `POST /api/Billing/webhooks/{provider}` — без user JWT |

REST: `/api/*` community routes, `/api/subscriptions`, `/api/Billing/*`.
