# Reader vocabulary statistics (term status, not card SRS)

## Context

Dashboard vocabulary stats previously counted **Mature / Learning / New** from card SRS progress (`UserCardProgress`) on cards linked to `ProjectTermId`. That diverged from the term-first Reader model:

- Reader and `/vocabulary` track knowledge via **`UserTermStatus`**: `NEW`, `SAVED`, `KNOWN`, `IGNORED`.
- A term marked **KNOWN** in Reader could still show **0** on the dashboard if no card had reached SRS maturity (21+ day interval).
- Product direction treats Reader as the primary learning surface; cards are optional SRS follow-up.

## Decision

**Vocabulary progress statistics come from `UserTermStatus` for the current user and project.**

| Metric | Source |
|--------|--------|
| **Total terms** | `KNOWN + SAVED/LINGQ + NEW` (ignored excluded) |
| **Known** | `Status == "KNOWN"` |
| **Saved (learning)** | `Status == "SAVED"` or legacy `"LINGQ"` |
| **New** | `Status == "NEW"` |
| **Ignored** | Excluded from totals and distribution |

CEFR level and estimated fluency are derived from **known** term count, not mature cards.

API field names remain unchanged for compatibility (`matureCount` carries known count; `learningCount` carries saved count).

## Consequences

- Dashboard, library banner, and retention rate align with Vocabulary page statuses.
- Card SRS metrics (due reviews, mature cards, retention from study) stay separate; do not mix into vocabulary-known counts.
- Terms without a card still appear in stats when they have a `UserTermStatus` row.
- Legacy `LINGQ` rows in the database count as saved/learning until migrated.

## Alternatives considered

1. **Keep card maturity as “known”** — rejected; contradicts Reader-first model and confuses users.
2. **Blend card SRS + term status** — rejected; double-counting and ambiguous semantics.
3. **Rename API only** — insufficient; algorithm had to change.

## Links

- Implementation: [`VocabularyService/Services/AnalyticsService.cs`](../../VocabularyService/Services/AnalyticsService.cs) — `GetVocabularyStatsAsync`
- Tests: [`VocabularyService.Tests/AnalyticsServiceVocabularyStatsTests.cs`](../../VocabularyService.Tests/AnalyticsServiceVocabularyStatsTests.cs)
- Domain rules: [`.cursor/rules/06-lingq-domain-guardrails.mdc`](../../.cursor/rules/06-lingq-domain-guardrails.mdc)
- Product mechanics: [`product-mechanics.md`](product-mechanics.md)
