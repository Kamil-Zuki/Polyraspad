# Review Task

Plan ID: `private-beta-readiness`
Agent: `reviewer-agent`
Status: pending
Can run in parallel: no — **run after** backend + frontend handoffs (or readonly gate on checklist now)

## Objective

Gate private beta: проверить P0 checklist, регрессии term-first, CI/deploy risks. Выдать go/no-go для первого beta-invite.

## Inputs

- Plan: `.cursor/plans/active/private-beta-readiness_a3f7b2c1.plan.md`
- Backend handoff: test fix, deploy.yml, runbook
- Frontend handoff: CI tests, build args
- LingQ guardrails: `.cursor/rules/06-lingq-domain-guardrails.mdc`
- Smoke: `DEV_RUNBOOK.md` § MVP Smoke Test + new § Private Beta Smoke

## Scope

### Readonly gate (можно до implementation)

Review P0 checklist completeness:

- [ ] All P0 items have owner + verify method
- [ ] Deferred items (plan 03/04) explicitly out of beta scope
- [ ] Known risks documented and acceptable

### Post-implementation gate

After backend + frontend deliverables:

- [ ] `VocabularyService.Tests` 55/55
- [ ] `AggregatorService.Tests` 49/49
- [ ] Frontend `npm test -- --run` 178/178
- [ ] `ci.yml` frontend includes test step
- [ ] `deploy.yml` changes safe (no accidental data loss beyond existing `git reset --hard`)
- [ ] No unsafe migrations introduced in this slice
- [ ] Term-first regressions not broken by test fix:
  - `sleep` / `slept` separate statuses
  - phrase duplicate logic intact

### Go / No-Go criteria

**GO for private beta** when:

- All P0 items #1–25 have evidence (checklist tick + verify output)
- CI green on merge commit
- At least one full P0-4 smoke pass on VPS documented

**NO-GO** if:

- Any P0 test failing on master
- Auth/register broken on prod URLs
- Media URLs 404 on prod

## Out of Scope

- Full LingQ acceptance criteria audit (plan 03/04 territory)
- Security pentest
- Load testing

## Deliverables

- [ ] Review report: GO / NO-GO / GO WITH CAVEATS
- [ ] P0 checklist with pass/fail per item (as far as verifiable from repo)
- [ ] List of remaining ops-only items for user
- [ ] P1/P2 recommendations ranked

## Verification

```powershell
dotnet test VocabularyService.Tests/VocabularyService.Tests.csproj -c Release
dotnet test AggregatorService.Tests/AggregatorService.Tests.csproj -c Release
cd polyraspad-frontend; npm test -- --run
```

## Handoff

Return to lead-agent:

- GO/NO-GO decision
- P0 item status table
- blockers requiring user action (VPS secrets, deploy trigger)
- suggested next plan after beta (plan 03 or 04)
