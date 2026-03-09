---
name: refactor-on-green
description: Performs safe refactoring after the current TDD increment is green. Use when duplication, naming, or structure should be improved without changing behavior, then hand control back to validation.
---

# Refactor on Green

## Entry condition

Enter only after the current red test is green.

Typical entry point:
- from `tdd-green`

## Refactoring priorities

1. Remove duplication created during the green step.
2. Improve names and intention-revealing structure.
3. Extract small helpers only when they clarify behavior.
4. Reduce branching or conditional noise where possible.

## Rules

- Do not change behavior during refactor.
- Re-run the narrowest useful tests after each meaningful refactor step.
- Prefer several tiny refactors over one large rewrite.
- If a refactor changes behavior, stop and go back to `tdd-red` or `tdd-green` through the orchestrator.

## Output

Produce a brief handoff with:

- what was simplified
- what tests remained green
- any residual cleanup intentionally deferred

Then route to `tdd-validation`.

## Exit condition

- Code is simpler than before.
- Tests still pass.
- The public contract is unchanged.
