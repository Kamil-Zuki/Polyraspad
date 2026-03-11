---
name: 00-autonomous-development
description: Coordinates autonomous delivery loops with sequencing, validation gates, retries, escalation, and verified stop conditions. Use when the user wants the agent to keep going until done, especially for autonomous Detroit TDD or end-to-end verified delivery.
---

# Autonomous Development

## When to use

Use when the user wants autonomous execution until the task is fully implemented and verified.

Canonical launch phrase:
`Run autonomous Detroit TDD for <task>. Keep going until verified.`

## Responsibility

This skill owns the outer loop only:

- define scope, constraints, and done criteria
- keep work moving in small validated increments
- require explicit validation before progress or completion
- route failures into the next bounded task
- stop only at a verified outcome

Do not use this skill as the detailed TDD stage playbook. Route that work to `01-tdd-orchestrator`.

## Outer loop

1. Define goal, scope, constraints, and done criteria.
2. Route the current increment to `01-tdd-orchestrator`.
3. Require a validation result before advancing scope.
4. If validation fails, route to `10-tdd-failure-recovery`.
5. If broader proof is needed, route to `11-tdd-integration-verifier` or `12-tdd-docs-contract-check`.
6. Repeat until done criteria are satisfied.

## Validation gate

After each meaningful change require:

- the narrowest useful test to pass
- nearby tests to stay green when relevant
- lints on edited files to stay clean
- behavior confirmation, not only compilation

If validation fails, treat that failure as the next scoped task instead of moving forward.

## Routing

- For Detroit TDD stage routing -> use `01-tdd-orchestrator`
- For failed validation recovery -> use `10-tdd-failure-recovery`
- For user-visible or cross-layer verification -> use `11-tdd-integration-verifier`
- For API, entity, or navigation alignment with `Docs/` -> use `12-tdd-docs-contract-check`
- For 13-commit/push requests -> use `13-commit`

## Stop only when

- all requested work is implemented
- acceptance criteria are satisfied
- relevant tests and checks are green, or blocked for a stated reason
- required integration or contract checks are complete
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
- Do not skip routing just because one stage seems obvious.
- If blocked, report the exact blocker and the furthest verified state.
