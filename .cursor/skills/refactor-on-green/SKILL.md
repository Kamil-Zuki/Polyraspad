---
name: refactor-on-green
description: Performs safe refactoring after tests are green in a TDD workflow. Use when simplifying design, removing duplication, or cleaning code without changing behavior.
---

# Refactor on Green

## Entry condition

Only use this skill when the relevant tests are already green.

## Refactoring priorities

1. Remove duplication created during the green step.
2. Improve names and intention-revealing structure.
3. Extract small helpers only when they clarify behavior.
4. Reduce branching or conditional noise where possible.

## Rules

- Do not change behavior during refactor.
- Re-run tests after every meaningful refactor step.
- Prefer several tiny refactors over one large rewrite.
- If a refactor changes behavior, stop and go back to TDD red-green.

## Exit condition

- Code is simpler than before.
- Tests still pass.
- The public contract is unchanged.
