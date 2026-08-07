# Фича: Магазин Колод и Сообщество (Marketplace & Subscriptions)

**Статус:** Implemented  
**Связанный бекенд:** Vocabulary Service (`CommunityService.GetMarketplaceCatalog`, `GetProductDetails`, `CreateReview`, `SubscriptionService.Subscribe`), Billing Service.

---

## 1. UX-сценарий (User Journey)

* **Шаг 1: Просмотр каталога (`/marketplace`).** Пользователь просматривает каталог опубликованных колод. Настраивает фильтры по языку, категории, цене (Free / Paid) и рейтингу.
* **Шаг 2: Страница товара (`/marketplace/products/[id]`).** Пользователь просматривает детали колоды, обложку, рейтинг, отзывы покупателей и превью первых карточек.
* **Шаг 3: Подписка / Скачивание.** Пользователь нажимает "Subscribe & Download". При необходимости проверяются права подписки через `BillingService`. Колода форкается в личную библиотеку пользователя.
* **Шаг 4: Управление подписками (`/subscriptions`).** Пользователь просматривает список скачанных колод, проверяет версию и при наличии обновлений от автора нажимает "Update Deck".

---

## 2. Маршрутизация и Страницы (Routing)

* `src/app/marketplace/page.tsx` — каталог товаров Marketplace.
* `src/app/marketplace/products/[id]/page.tsx` — карточка товара.
* `src/app/subscriptions/page.tsx` — список подписок на скачанные колоды.

---

## 3. Дерево компонентов (Component Architecture)

```
<MarketplaceCatalogPage> (Client)
├── <CatalogFilterBar> — фильтры по категориям, языкам и поисковой строке
└── <ProductGrid> — сетка товаров
    └── <ProductCard> — карточка колоды с рейтингом, автором и ценой

<ProductDetailPage> (Client)
├── <ProductHeader> — заголовок, обложка, кнопка "Subscribe"
├── <DeckPreviewCarousel> — карусель превью заметок карточки
└── <ReviewSection> — отзывы пользователей и форма добавления отзыва
```

---

## 4. Интеграция с API (Data Fetching & BFF)

* **Чтение (Queries):**
  * `GET /api/v1/marketplace/catalog` (`CommunityService.GetMarketplaceCatalog`) — каталог колод.
  * `GET /api/v1/marketplace/products/{id}` (`CommunityService.GetProductDetails`) — детали товара.
  * `GET /api/v1/subscriptions` (`SubscriptionService.ListSubscriptions`) — подписки пользователя.
* **Мутации (Mutations):**
  * `POST /api/v1/marketplace/subscribe` (`SubscriptionService.Subscribe`) — подписка на колоду.
  * `POST /api/v1/marketplace/reviews` (`CommunityService.CreateReview`) — добавление отзыва.

---

## 5. Управление состоянием (State Management)

* **Локальное состояние:**
  * `catalogFilters`: текущие параметры фильтрации каталога.
* **Кэш React Query:**
  * `['marketplace', 'catalog', filters]` — кэш страниц каталога.
  * `['subscriptions', userId]` — кэш подписок. Инвалидируется при подписке/отписке.

---

## 6. Стратегия тестирования фронтенда (UI Testing)

* **Компонентные тесты (`src/components/marketplace/marketplace.test.tsx`):**
  * Проверка фильтрации сетки товаров при вводе ключевых слов.
  * Проверка вызова `Subscribe` и добавления бейджа "Subscribed".
  * Проверка формы отправки отзыва и валидации оценки (1-5 звезд).
