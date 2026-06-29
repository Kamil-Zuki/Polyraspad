---
name: private-beta-readiness
overview: Подготовить Polyraspad к limited private beta на VPS (polyraspad.online) — закрыть P0 блокеры infra/CI/tests, усилить deploy и smoke, зафиксировать scope без public launch.
todos:
  - id: p0-vocabulary-test-fix
    content: "P0: Починить failing test StudyServiceProjectScopedProgressTests в VocabularyService.Tests"
    status: pending
  - id: p0-prod-env-vps
    content: "P0: Заполнить production .env на VPS (JWT, SMTP, CORS, NEXT_PUBLIC_*, MinIO URLs, AI_PROXY_API_KEY)"
    status: pending
  - id: p0-ci-green
    content: "P0: Убедиться что CI на master зелёный (SUBMODULES_PAT, все jobs включая docker)"
    status: pending
  - id: p0-post-deploy-smoke
    content: "P0: Расширить post-deploy smoke (auth + reader + card + media URL) — см. plan § Post-Deploy Smoke"
    status: pending
  - id: p1-frontend-tests-ci
    content: "P1: Добавить npm test -- --run в ci.yml для polyraspad-frontend"
    status: pending
  - id: p1-deploy-hardening
    content: "P1: Deploy hardening — SSH key вместо password, убрать --no-cache по умолчанию, document rollback"
    status: pending
  - id: p1-beta-smoke-runbook
    content: "P1: Расширить DEV_RUNBOOK § MVP Smoke Test секцией Private Beta Smoke"
    status: pending
  - id: p2-docs-stale-roadmap
    content: "P2: Обновить устаревшие Docs (aggregator-bridge-audit, reader-library-lingq-roadmap Phase 0)"
    status: pending
  - id: p2-onboarding-deferred
    content: "P2 (deferred): Onboarding seed content — plan 04, не блокер private beta"
    status: cancelled
  - id: p2-srs-reader-deferred
    content: "P2 (deferred): Review из Reader — plan 03, не блокер private beta"
    status: cancelled
isProject: false
---

# Private Beta Readiness

Plan ID: `private-beta-readiness`
Status: **active**
Created: 2026-06-27
Owner: `lead-agent`

## Goal

Довести Polyraspad до **limited private beta** на VPS (`polyraspad.online` / `app.polyraspad.online` / `api.polyraspad.online`): инфраструктура стабильна, CI зелёный, post-deploy smoke проходит, beta-пользователи могут регистрироваться, читать, создавать карточки и проходить study session.

**Не цель этого плана:** public launch, onboarding seed, SRS-from-reader, YooKassa production.

## Launch Mode Decision

| Режим | Готовность (оценка 2026-06-27) | Решение |
|-------|----------------------------------|---------|
| Local dev | ~85% | ✅ готов |
| **Private beta (этот план)** | ~68% → target **≥90%** | 🎯 in scope |
| Public LingQ-style launch | ~52% | ❌ out of scope |

## Out of Scope

- Plan 03: Review из Reader (SRS entry из reader header)
- Plan 04: Onboarding + seed library (2–3 дефолтных текста)
- YooKassa production payments (beta: `BILLING_DEFAULT_PROVIDER=mock` допустим)
- OCR для PDF, PWA polish, content-first library Phase 3
- E2E Playwright в CI

## Agents

| Agent | Ответственность |
|-------|-----------------|
| `backend-agent` | P0 test fix, CI gaps (auth/media tests опционально), deploy script improvements, post-deploy curl smoke |
| `frontend-agent` | P1 frontend tests в CI, prod build args verification, beta smoke UI paths |
| `reviewer-agent` | Gate checklist перед первым beta-invite; регрессии term-first |
| `product-agent` | **не нужен** — scope зафиксирован в этом плане |

## Tasks

- `.cursor/tasks/active/private-beta-readiness/backend.md`
- `.cursor/tasks/active/private-beta-readiness/frontend.md`
- `.cursor/tasks/active/private-beta-readiness/review.md`

---

# Pre-Launch Checklist (P0 / P1 / P2)

Легенда: **Owner** = кто закрывает; **Verify** = как проверить.

## P0 — Blockers (без этого beta нельзя)

### P0-1. Production secrets и URLs на VPS

| # | Item | Owner | Verify |
|---|------|-------|--------|
| 1 | `POSTGRES_PASSWORD` — не дефолт | ops | `docker compose exec postgres printenv` |
| 2 | `JWT_SECRET` ≥ 32 chars, одинаковый в auth + aggregator | ops | login + token refresh работает |
| 3 | `SMTP_*` + `AUTH_CONFIRMATION_LINK=https://api.polyraspad.online/api/Auth/confirm-email?userId` | ops | регистрация → письмо → confirm |
| 4 | `CORS_ALLOWED_ORIGINS=https://app.polyraspad.online` (+ landing если нужно) | ops | login из браузера без CORS error |
| 5 | `NEXT_PUBLIC_API_URL=https://api.polyraspad.online` | ops/frontend | Network tab: API calls на prod URL |
| 6 | `NEXT_PUBLIC_APP_URL=https://app.polyraspad.online` | ops/frontend | auth redirect корректен |
| 7 | `MINIO_PUBLIC_BASE_URL=https://api.polyraspad.online/polyraspad-media` | ops | картинка на карточке грузится |
| 8 | `AI_PROXY_API_KEY` задан (если AI features в beta) | ops | `/api/ai/models` отвечает |
| 9 | `BILLING_DEFAULT_PROVIDER=mock` явно (beta без реальных платежей) | ops | billing UI показывает mock flow |

### P0-2. CI green on master

| # | Item | Owner | Verify |
|---|------|-------|--------|
| 10 | GitHub secret `SUBMODULES_PAT` (classic PAT, `repo` scope) | ops | CI checkout submodules без 401 |
| 11 | Все CI jobs pass (включая `docker compose build`) | backend | GitHub Actions green |
| 12 | **Fix:** `VocabularyService.Tests` — 1 failing test | backend | `dotnet test VocabularyService.Tests` → 55/55 |

Failing test (2026-06-27):

```
StudyServiceProjectScopedProgressTests.GetNextCardAsync_PassesFsrsProgress_FromSessionProject_WhenDuplicateRowsExist
```

### P0-3. Deploy pipeline работает

| # | Item | Owner | Verify |
|---|------|-------|--------|
| 13 | `workflow_dispatch` deploy на VPS успешен | ops | deploy.yml completes |
| 14 | `git submodule update --init --recursive` на VPS | ops | submodules на expected commits |
| 15 | `curl -fsS http://127.0.0.1:5000/healthz` после deploy | ops | `{"status":"ok"}` |
| 16 | nginx SSL valid для всех трёх доменов | ops | `curl -I https://app.polyraspad.online` → 200 |

### P0-4. Post-Deploy Smoke (минимум для beta)

Прогон **на VPS после каждого deploy** (ручной или scripted):

| # | Flow | Verify |
|---|------|--------|
| 17 | Frontend opens | `https://app.polyraspad.online` → 200 |
| 18 | Register + email confirm | новый аккаунт активен |
| 19 | Login + `GET /api/Auth/me` | 200 с user |
| 20 | Create/open deck | deck list работает |
| 21 | Create/update card + image | media URL resolves |
| 22 | Study session (1 card, rate Good) | FSRS interval обновился |
| 23 | Reader: import TXT/EPUB, analyze, click word | inspector открывается |
| 24 | Reader: Create LingQ (Save term) | слово жёлтое |
| 25 | Billing page loads (mock) | нет 500 |

---

## P1 — Should Have (beta quality)

### P1-1. CI gates

| # | Item | Owner | Verify |
|---|------|-------|--------|
| 26 | `npm test -- --run` в `ci.yml` frontend job | frontend | CI fails if tests fail |
| 27 | (Optional) `AgentService.Tests` already in CI ✓ | — | done |
| 28 | (Optional) add `authorization-module` smoke test job | backend | future |

### P1-2. Deploy hardening

| # | Item | Owner | Verify |
|---|------|-------|--------|
| 29 | SSH key auth вместо `VPS_PASSWORD` | ops | deploy без password secret |
| 30 | Убрать `docker compose build --no-cache` по умолчанию | backend/ops | deploy time разумный |
| 31 | Document rollback: `git reset --hard <prev-sha>` + rebuild | ops | runbook entry |
| 32 | Pre-deploy: CI green on target commit | ops | manual gate |

### P1-3. Observability & recovery

| # | Item | Owner | Verify |
|---|------|-------|--------|
| 33 | `docker compose ps` все сервисы Up | ops | post-deploy |
| 34 | Log tail playbook в DEV_RUNBOOK | backend | § Private Beta Smoke |
| 35 | Backup Postgres volume procedure documented | ops | one-pager |

### P1-4. Beta UX expectations

| # | Item | Owner | Verify |
|---|------|-------|--------|
| 36 | Beta banner / known limitations (optional) | frontend | UI note |
| 37 | `mark known on page turn` = localStorage (documented) | product/docs | no surprise |
| 38 | PDF без текста → explicit error panel | frontend | already in plan 01 ✓ |

---

## P2 — Nice to Have (before public, not beta blockers)

| # | Item | Owner | Notes |
|---|------|-------|-------|
| 39 | Update `Docs/architecture/aggregator-bridge-audit.md` | docs | Phase 0 done |
| 40 | Update `Docs/reader-library-lingq-roadmap.md` gaps | docs | controllers exist |
| 41 | Plan 03: Review из Reader | product+frontend | deferred |
| 42 | Plan 04: Onboarding seed | product+backend | deferred |
| 43 | YooKassa production + webhook key | backend | monetization |
| 44 | Staging environment | ops | pre-prod |
| 45 | Upgrade Npgsql/MimeKit (NU1902/NU1903 warnings) | backend | security |
| 46 | E2E Playwright smoke | frontend | post-beta |
| 47 | Frontend test count gate (178 tests baseline) | frontend | regression |

---

## Known Risks (accept for beta)

| Risk | Mitigation |
|------|------------|
| EF migrations at container startup | Review each migration before deploy; backup DB |
| gRPC h2c plaintext inside Docker network | OK for single VPS; not for multi-tenant cloud |
| Billing mock — no real revenue | Explicit beta scope; no payment promises |
| SRS not linked from Reader | Study via `/study/[deckId]` still works |
| No onboarding seed content | Beta users import own content |

## Contracts To Lock

- Production URLs: `NEXT_PUBLIC_*`, `CORS`, `MINIO_PUBLIC_BASE_URL`, `AUTH_CONFIRMATION_LINK`
- Beta billing: `BILLING_DEFAULT_PROVIDER=mock`
- Smoke script paths: `DEV_RUNBOOK.md` § Private Beta Smoke (P1 deliverable)

## Verification (plan close)

```powershell
# Backend
dotnet test VocabularyService.Tests/VocabularyService.Tests.csproj -c Release
dotnet test AggregatorService.Tests/AggregatorService.Tests.csproj -c Release

# Frontend
cd polyraspad-frontend; npm test -- --run

# Infra (on VPS after deploy)
curl -fsS https://api.polyraspad.online/healthz
# + manual P0-4 smoke table
```

## Execution Order

1. **backend-agent** — P0-2 (#12 test fix), P1 deploy script (#30), optional post-deploy script
2. **frontend-agent** — P1-1 (#26 CI tests), verify prod build args
3. **reviewer-agent** — gate P0 checklist, term-first regression spot-check
4. **ops/manual** — P0-1 secrets, P0-3 deploy, P0-4 smoke on VPS

## Cleanup

- [ ] All P0 items checked
- [ ] Frontmatter todos `completed` or `cancelled`
- [ ] Tasks → `.cursor/tasks/archive/private-beta-readiness/`
- [ ] Plan → `.cursor/plans/archive/private-beta-readiness_a3f7b2c1.plan.md`
