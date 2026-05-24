# Reader vocabulary statistics (Known Terms model)

## Context

Dashboard vocabulary stats need to answer one primary question: **how many terms does the user know?** That number drives CEFR level, known share, and estimated fluency.

Two data sources exist:

- **Reader / Vocabulary** track knowledge via `UserTermStatus`: `NEW`, `SAVED`, `KNOWN`, `IGNORED`.
- **FSRS cards** track optional SRS follow-up via `UserCardProgress` (`State`, `ScheduledDays`, `Due`, etc.).

A term marked **KNOWN** in Reader should count toward level even without a card. A term with a **mature FSRS card** should also count as known even if Reader status is still `NEW` or `SAVED`.

## Decision

**Dashboard level uses a single primary metric: Known Terms.**

| Metric | Source | Role |
|--------|--------|------|
| **Known** | `UserTermStatus == "KNOWN"` **or** linked FSRS card is mature | Primary level metric (CEFR, fluency, known share) |
| **In Review** | Linked FSRS card exists and is not mature | Learning pipeline |
| **Saved** | `SAVED` / `LINGQ` / `LEARNING` without stronger known/review signal | Learning pipeline |
| **New** | `NEW` without stronger card/status signal | Learning pipeline |
| **Ignored** | Excluded from totals and distribution | — |
| **Total** | Known + In Review + Saved + New | — |

Classification priority per term: `Ignored → Known → In Review → Saved → New`.

### FSRS mature definition

FSRS does not store a separate "mature" state. Mature is derived at read time:

- `State == 2` (Review)
- `ScheduledDays >= 21`

Use `ScheduledDays`, not `Due >= now + 21 days`, so overdue mature review cards still count as known.

### API compatibility

Public field names remain unchanged:

- `matureCount` carries Known Terms count
- `learningCount` carries `savedCount + reviewingCount` for backward compatibility

## Consequences

- Dashboard, library banner, and CEFR progress align on one "known vocabulary" number.
- In Review / Saved / New explain what is still in the learning pipeline without competing with the level metric.
- Card SRS operational metrics (due queue, retention from study sessions) stay separate from vocabulary level estimation.
- Terms without a card still appear in stats when they have a `UserTermStatus` row.
- Legacy `LINGQ` rows in the database count as saved until migrated.

## Alternatives considered

1. **Term status only (Reader KNOWN)** — rejected; ignores FSRS evidence when users learn primarily through cards.
2. **FSRS mature only** — rejected; contradicts Reader-first model and undercounts terms marked known without cards.
3. **Split Known into two equal dashboard metrics** — rejected; the product question is level, not source accounting.
4. **Rename API only** — insufficient; algorithm and copy had to change together.

## Links

- Implementation: [`VocabularyService/Services/AnalyticsService.cs`](../../VocabularyService/Services/AnalyticsService.cs) — `GetVocabularyStatsAsync`
- UI: [`polyraspad-frontend/src/components/analytics/vocabulary-stats.tsx`](../../polyraspad-frontend/src/components/analytics/vocabulary-stats.tsx)
- Tests: [`VocabularyService.Tests/AnalyticsServiceVocabularyStatsTests.cs`](../../VocabularyService.Tests/AnalyticsServiceVocabularyStatsTests.cs)
- Domain rules: [`.cursor/rules/06-lingq-domain-guardrails.mdc`](../../.cursor/rules/06-lingq-domain-guardrails.mdc)
