# Billing Service — документация микросервиса

SaaS-биллинг платформы Polyraspad: provider-agnostic ядро, каталог планов, подписки, entitlements, инвойсы и webhook-оркестрация. Публичный REST — через **AggregatorService**; межсервисный контракт — **gRPC** (порт `5127`).

## Структура

| Папка | Содержание |
| :--- | :--- |
| [[01 - Функциональная спецификация]] | SR-группы и сервисные требования (`SR-BILL-*`) |
| [[02 - Архитектура]] | КАР, слои, интеграции |
| [[03 - Модель Данных]] | PostgreSQL schema `billing`, сущности EF Core |
| [[04 - Бекенд, API и Контракты]] | gRPC, DTO (batch — по правилам `Docs/.cursor/rules/` для папки 04) |
| [[99 - Staging — Разрывы согласованности (DO NOT DELETE)]] | ISSUE при расхождениях `01`↔`03` |

## Эталон формата

- Полный образец: `(Done) Authorization Service/` — **layout only**
- Правила: `Docs/.cursor/rules/`

## Код

Реализация: `BillingService/` (submodule, .NET 8, gRPC `5127`, PostgreSQL `billing_service`).

## Отличия от marketplace billing

- **Deck subscriptions** и **UserEntitlement** для marketplace — в `VocabularyService` (`SubscriptionService`).
- Billing Service отвечает **только** за SaaS-тарифы платформы (`free`, `pro`, …).
