# Backend Agent Task

Plan ID: `reader-library-lingq-roadmap-2026-05-13`
Agent: `backend-agent`
Status: done
Can run in parallel: yes

## Objective

Реализовать и стабилизировать **Phase 0 REST bridge** в Aggregator и связанные клиенты (в т.ч. `MediaServiceClientImpl`, регистрация DI), затем точечные расширения для Phase 1–2 по мере появления зафиксированных контрактов.

## Inputs

- Plan: `.cursor/plans/active/reader-library-lingq-roadmap-2026-05-13.md`
- Files/contracts to read:
  - `Docs/api/reader-aggregator-contract.md`
  - `Docs/architecture/aggregator-bridge-audit.md`
  - `AggregatorService/` (контроллеры, DI, существующие клиенты)
  - `VocabularyService/` (gRPC, существующие вызовы)
  - Media service integration points (по аудиту)

## Scope

- **Phase 0:** `TextController` + `POST /api/text/analyze`; `TermsController` + term endpoints согласно контракту и фронтовым константам; `MediaController` + reader library endpoints; исправление регистрации `MediaServiceClientImpl` в DI.
- Проксирование/агрегация в VocabularyService/MediaService без прямого доступа к чужим БД (границы сервисов).
- Интеграционные тесты для новых HTTP endpoints (WebApplicationFactory / существующий паттерн репо).
- **Phase 1 (после замка контракта):** поддержка user setting для bulk known, если хранится на backend; убедиться что bulk endpoint идемпотентен/безопасен при повторе.
- **Phase 2:** API для review из текущего контекста и привязка source URL к карточкам — только после отдельного среза контракта (обновить план при старте).

## Out of Scope

- Minimal API вместо controller-based (запрещено правилами репо).
- Новая логика на LemmaId для статусов/дубликатов.

## Deliverables

- Рабочие контроллеры и клиенты; зелёные интеграционные тесты на критические маршруты Phase 0.
- Обновление `Docs/api/reader-aggregator-contract.md` только если фактический контракт изменился и это согласовано с фронтом.

## Verification

- `dotnet test` на затронутых `*.Tests` проектах Aggregator (узкий фильтр по имени контроллера/сборке).
- Ручная проверка из roadmap DoD: `POST /api/text/analyze` → 200; `POST /api/terms` → 201; `GET` library path → 200 (точные пути из контракта/констант).

## Handoff

- Список реализованных маршрутов + отличия от документа; известные ограничения Media client для `frontend-agent`.
