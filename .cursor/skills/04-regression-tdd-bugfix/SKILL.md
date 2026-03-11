---
name: 04-regression-tdd-bugfix
description: Defines the regression-first TDD policy for defects and broken behavior. Use when triage classifies the next increment as a bug, regression, production issue, or incorrect edge case.
---

# Regression-First Bugfix Playbook

## When to use

Use only when `02-tdd-triage` classifies the next increment as previously broken behavior.

## Entry

Enter from `02-tdd-triage` with:

- the broken scenario
- the expected behavior
- the highest useful reproduction boundary

## Policy

- Reproduce the bug at the highest useful public boundary.
- Keep the regression test permanently.
- Fix the root cause, not only the visible symptom.
- If the bug is boundary-related, add one adjacent edge-case scenario when needed.
- Keep the increment narrow and avoid broad refactors before green.

## Output

Produce a concise bugfix policy handoff for the next stage:

- what scenario is broken
- what correct behavior must be protected
- what boundary should reproduce it

Then route to `05-tdd-test-strategy`.

## Guardrails

- Do not implement the fix before `06-tdd-red` proves the regression is red.
- Do not delete or weaken the regression test after the fix.
- If reproduction is ambiguous, clarify expected behavior before proceeding.
