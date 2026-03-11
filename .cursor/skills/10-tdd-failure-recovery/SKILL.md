---
name: 10-tdd-failure-recovery
description: Converts failed validation or broken TDD progress into the next bounded task. Use when tests, lints, or verification fail and the work must be routed back into triage, strategy, red, or green instead of open-ended debugging.
---

# TDD Failure Recovery

## When to use

Use whenever `tdd-validation` reports failure or a TDD stage cannot proceed cleanly.

## Responsibilities

1. Identify the smallest cause category:
   - incorrect production behavior
   - malformed or mis-scoped test
   - missing fixture or boundary setup
   - environment or tooling blocker
2. Turn that cause into one bounded next task.
3. Route the work back to the earliest necessary stage.

## Output

Produce a concise handoff with:

- failure category
- smallest bounded next task
- next route: `02-tdd-triage`, `05-tdd-test-strategy`, `06-tdd-red`, or `07-tdd-green`

## Guardrails

- Do not switch into broad exploratory debugging.
- Do not restart the whole flow if a smaller rollback is enough.
- Escalate only when the blocker changes implementation direction or requires user input.
