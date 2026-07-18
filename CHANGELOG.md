# Changelog (История изменений)

Все заметные изменения в платформе Polyraspad (микросервисы, frontend, агенты) документируются в этом файле.
Формат основан на стандарте [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/).

## [Unreleased] - MVP (Этап 1)

### Planned (В планах на реализацию)
- **Feature Flags:** Скрытие продвинутого функционала AI-агентов (Inspector, Assistant, Auto-drafts, AI Audio) и сложных модулей (Lessons, Marketplace, Shadowing) под флаги `NEXT_PUBLIC_FF_AI_AGENTS` и `NEXT_PUBLIC_FF_ADVANCED_MODULES` для чистого MVP релиза.
- **Billing:** Интеграция провайдера ЮKassa в `BillingService` и `AggregatorService` для приема первых платежей в РФ.
- **Reader & FSRS:** Финализация базового цикла обучения (LingQ -> FSRS). Настройка строгой синхронизации статусов карточек (New, Saved, Known) между читалкой и тренажером.
- **Extension:** Финализация расширения Chrome для сбора фраз и субтитров с отсечением дубликатов на сервере.
