---
name: regression-tdd-bugfix
description: Fixes bugs with regression-first TDD. Use when the user reports a bug, failing scenario, edge-case defect, or asks for a safe bug fix.
---

# Regression-First Bugfix TDD

## When to use

Use for defects, regressions, production issues, and incorrect edge-case behavior.

## Workflow

1. Reproduce the bug at the highest useful public boundary.
2. Add a regression test that fails on current behavior.
3. Narrow the failure to the root cause if needed.
4. Implement the smallest fix.
5. Re-run the regression and nearby tests.
6. Refactor only after the suite is green.

## Rules

- Keep the regression test permanently.
- Fix the root cause, not only the visible symptom.
- If the bug is a boundary issue, add one adjacent edge-case test too.
- Do not mix large refactors into the fix commit.
- If reproduction is ambiguous, first clarify the expected behavior.

## Good outcomes

- The original bug becomes impossible to reintroduce silently.
- The test name describes the broken scenario and expected behavior.
