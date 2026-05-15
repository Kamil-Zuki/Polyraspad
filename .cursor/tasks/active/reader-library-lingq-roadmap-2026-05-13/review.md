# Reviewer Agent Task

Plan ID: `reader-library-lingq-roadmap-2026-05-13`
Agent: `reviewer-agent`
Status: done
Can run in parallel: no

## Objective

После завершения срезов Phase 0 и Phase 1 (и далее по запросу `lead-agent`) провести архитектурный и регрессионный обзор: контракты, term-first инварианты, тестовые пробелы, риски миграций.

## Inputs

- Plan: `.cursor/plans/active/reader-library-lingq-roadmap-2026-05-13.md`
- Files/contracts to read:
  - `Docs/testing/reader-library-tdd-matrix.md`
  - `.cursor/rules/06-lingq-domain-guardrails.mdc`
  - Diff / затронутые PR-области от `backend-agent` и `frontend-agent`

## Scope

- Чеклист регрессий: `sleep`/`slept` разные статусы; `go`/`went` не дубли карточек; фраза «take off» vs отдельные слова; bulk known только при включённой настройке; нет lemma labels в reader; phrase highlight > word highlight.
- Соответствие REST/gRPC DTO и фронтового клиента.
- Миграции: только неразрушающие, с backfill планом.

## Out of Scope

- Написание всего нового функционала вместо владельцев областей.

## Deliverables

- Краткий отчёт: **ship / ship with fixes / hold** с перечнем findings по серьёзности.

## Verification

- Просмотр CI и локальных тестов, рекомендованных в плане.

## Handoff

- Блокирующие issues для `lead-agent`; необязательные follow-ups для backlog.
