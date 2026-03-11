---
name: 09-tdd-validation
description: Runs the validation gate for the current TDD increment, including narrow tests, nearby checks, lints, and completion routing. Use when deciding whether work can advance, needs broader verification, or must go into recovery.
---

# TDD Validation

## When to use

Use after `07-tdd-green`, `08-refactor-on-green`, integration verification, or docs checking.

## Responsibilities

1. Re-run the narrowest useful test scope first.
2. Run nearby tests when the change could affect adjacent behavior.
3. Check lints for edited files.
4. Decide whether the increment is complete, needs another TDD cycle, needs broader verification, or failed validation.

## Output

Produce a concise handoff with:

- validation status: `pass`, `fail`, or `needs-broader-check`
- checks performed
- remaining gap or next route

## Next route

- `pass` with more scope remaining -> `02-tdd-triage`
- `pass` with broader proof needed -> `11-tdd-integration-verifier` or `12-tdd-docs-contract-check`
- `fail` -> `10-tdd-failure-recovery`

## Guardrails

- Do not treat compilation alone as enough validation.
- Do not skip nearby regression checks when the risk is obvious.
- Do not declare done while known gaps remain.
