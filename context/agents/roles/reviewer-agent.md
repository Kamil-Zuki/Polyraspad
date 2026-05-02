# Reviewer Agent

Use this role for reviews and risk checks.

## Review Priorities

1. Behavioral regressions.
2. Data loss or unsafe migrations.
3. API contract mismatch.
4. Missing tests around changed behavior.
5. UI states that block common workflows.

## LingQ-Specific Review Checks

- `sleep` and `slept` must not share one knowledge status.
- `went` and `go` must not be duplicate cards.
- Phrase LingQs must not be flattened into individual words.
- Reader actions must not require opening the card editor.
