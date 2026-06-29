# Backend Task

Plan ID: `private-beta-readiness`
Agent: `backend-agent`
Status: pending
Can run in parallel: yes (with frontend CI task)

## Objective

Закрыть P0/P1 backend-блокеры private beta: починить failing VocabularyService test, усилить deploy workflow, подготовить post-deploy smoke helpers.

## Inputs

- Plan: `.cursor/plans/active/private-beta-readiness_a3f7b2c1.plan.md`
- Failing test: `VocabularyService.Tests/StudyServiceProjectScopedProgressTests.cs` → `GetNextCardAsync_PassesFsrsProgress_FromSessionProject_WhenDuplicateRowsExist`
- Deploy: `.github/workflows/deploy.yml`
- CI: `.github/workflows/ci.yml`
- Runbook: `DEV_RUNBOOK.md`

## Scope (P0 + P1 backend-owned)

### P0-2 #12 — Fix failing test

- Починить `StudyServiceProjectScopedProgressTests.GetNextCardAsync_PassesFsrsProgress_FromSessionProject_WhenDuplicateRowsExist`
- Root cause: `StudyService.CalculateQueueStatsAsync` — `ToDictionary` duplicate key при duplicate FSRS rows
- Добавить/сохранить регрессионный тест; не менять публичный API без необходимости

### P1-2 #30 — Deploy workflow

- Убрать `--no-cache` из `docker compose build` в `deploy.yml` (или сделать opt-in через input)
- (Optional P1-2 #29) Document migration to SSH key — не менять secrets без доступа пользователя

### P1-3 #34 — Runbook

- Добавить в `DEV_RUNBOOK.md` секцию **Private Beta Smoke** — таблица P0-4 из плана + команды `docker compose logs`

### Optional

- Shell script `scripts/post-deploy-smoke.sh` (curl healthz + auth me if token provided)
- Не трогать billing/YooKassa в этом slice

## Out of Scope

- Plan 03/04 product features
- authorization-module package upgrades (P2 #45)
- Frontend CI (frontend-agent)
- VPS secrets configuration (manual ops)

## Deliverables

- [ ] `VocabularyService.Tests` — 55/55 green
- [ ] `deploy.yml` — без `--no-cache` по умолчанию
- [ ] `DEV_RUNBOOK.md` — § Private Beta Smoke
- [ ] (Optional) `scripts/post-deploy-smoke.sh`

## Verification

```powershell
dotnet test VocabularyService.Tests/VocabularyService.Tests.csproj -c Release --verbosity normal
dotnet test AggregatorService.Tests/AggregatorService.Tests.csproj -c Release --verbosity minimal
```

## Handoff

Return to lead-agent:

- files changed
- root cause + fix summary for failing test
- deploy.yml diff rationale
- verification results (pass/fail counts)
- blockers for ops-only items (VPS secrets)
