# Frontend Task

Plan ID: `private-beta-readiness`
Agent: `frontend-agent`
Status: pending
Can run in parallel: yes (with backend test fix)

## Objective

Закрыть P1 frontend gates для private beta: добавить Vitest в CI, верифицировать production build args, опционально beta UX note.

## Inputs

- Plan: `.cursor/plans/active/private-beta-readiness_a3f7b2c1.plan.md`
- CI: `.github/workflows/ci.yml` — job `frontend`
- Frontend tests baseline: **178 tests / 52 files** (all pass locally 2026-06-27)
- Prod URLs: `.env.example` — `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_APP_URL`
- Docker build args: `docker-compose.yml` → `polyraspad-frontend` service

## Scope

### P1-1 #26 — CI test gate

- Добавить step после `npm ci` в CI job `frontend`:

```yaml
- name: Test
  working-directory: polyraspad-frontend
  run: npm test -- --run
```

- Убедиться что CI env не ломает тесты (mock API URLs ok)

### P1-4 #36 — Beta UX (optional, minimal)

- Если уместно: небольшой banner «Private Beta» на dashboard или settings — **только если не раздувает scope**
- Иначе: document in handoff «not needed»

### Verify production build contract

- Confirm `docker-compose.yml` passes `NEXT_PUBLIC_API_URL` / `NEXT_PUBLIC_APP_URL` as build args
- Document required VPS values in handoff (no code change if already correct)

## Out of Scope

- Plan 03 Review from Reader UI
- Plan 04 onboarding seed
- E2E Playwright
- Billing YooKassa UI changes
- Large reader refactors

## Deliverables

- [ ] `ci.yml` — frontend test step
- [ ] Local verify: `npm test -- --run` → 178 passed
- [ ] Handoff: prod URL checklist for ops

## Verification

```powershell
cd polyraspad-frontend
npm test -- --run
npm run build
# env for prod-like build:
$env:NEXT_PUBLIC_API_URL="https://api.polyraspad.online"
$env:NEXT_PUBLIC_APP_URL="https://app.polyraspad.online"
npm run build
```

## Handoff

Return to lead-agent:

- files changed
- CI step added
- test count baseline
- prod build args confirmation
- optional beta banner decision
