---
name: autonomous-development
description: Executes full-cycle autonomous development with iterative plan, implementation, validation, fixes, and final verification. Use when the user wants the agent to keep going until the task is fully done, especially for end-to-end delivery, autonomous execution, or "don't stop until it works" requests.
---

# Autonomous Development

## When to use

Use when the user wants the agent to keep going until the task is fully done: implement, validate, fix, repeat, and stop only at a verified outcome.

## Core loop

1. Define goal, scope, and done criteria.
2. Break work into the smallest meaningful next step.
3. Execute that step.
4. Validate immediately with the narrowest useful check.
5. Fix the next discovered problem before continuing.
6. Repeat until done criteria are satisfied.

## Validation

After each meaningful change:

- run targeted tests first
- run broader validation when needed
- check lints for edited files
- confirm behavior, not just compilation

If validation fails, treat that failure as the next task and re-run the relevant checks after the fix.

## Routing

- For TDD execution -> use `tdd-orchestrator`
- For commit/push requests -> use `commit`

## Stop only when

- all requested work is implemented
- acceptance criteria are satisfied
- relevant tests/checks are green, or blocked for a stated reason
- no known loose ends remain inside current scope

## Escalate when

- requirements are ambiguous and change the implementation direction
- a destructive or risky action is required
- unexpected unrelated changes appear
- access, credentials, or external services block completion
- multiple valid product decisions exist and the choice matters

## Guardrails

- Do not expand scope just because adjacent improvements exist.
- Prefer small validated loops over large speculative edits.
- Do not stop at "implemented"; stop at "implemented and verified".
- If blocked, report the exact blocker and the furthest verified state.
