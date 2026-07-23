# Changelog (История изменений)

Все заметные изменения в платформе Polyraspad (микросервисы, frontend, агенты) документируются в этом файле.
Формат основан на стандарте [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/).

## [Unreleased]

## [0.1.0] - 2026-07-18 - MVP (Этап 1)

### Added (Добавлено)
- **Billing:** Интеграция провайдера ЮKassa в `BillingService` и `AggregatorService` для приема платежей в РФ, реализована страница выбора тарифа `/billing`.
- **Extension:** Настроен эндпоинт `CaptureCard` для сбора фраз и субтитров из расширения Chrome с проверкой на точные дубликаты.

### Changed (Изменено)
- **Feature Flags:** Скрыт продвинутый функционал AI-агентов (Inspector, Assistant, Auto-drafts, AI Audio) и сложные модули (Lessons, Marketplace, Shadowing) под флаги `NEXT_PUBLIC_FF_AI_AGENTS` и `NEXT_PUBLIC_FF_ADVANCED_MODULES` для MVP релиза.
- **Reader & FSRS:** Финализирован базовый цикл LingQ -> FSRS. Усилена консистентность статусов карточек (SAVED, KNOWN), обеспечено разделение форм слов (напр. sleep/slept) без агрессивной лемматизации.

