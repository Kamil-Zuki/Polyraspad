---
name: tdd-orchestrator
description: Orchestrates full TDD workflow across feature work, bugfixes, test design, test doubles, and refactoring. Use when the user asks to implement via TDD, Detroit TDD, regression-first TDD, or wants step-by-step red-green-refactor execution.
---

# TDD Orchestrator

## When to use

Use when the user wants:

- TDD
- Detroit TDD
- red-green-refactor
- regression-first bug fixing
- step-by-step test-driven implementation

## Routing

- **New feature / new behavior** -> use `detroit-tdd-feature`
- **Bug / regression / defect** -> use `regression-tdd-bugfix`
- **Need help designing tests** -> use `tdd-test-design`
- **Need to decide mocks/fakes/real objects** -> use `test-doubles-boundaries`
- **Code is green and needs cleanup** -> use `refactor-on-green`
- **Ambiguous task** -> first determine whether expected behavior is new or previously broken

## Execution flow

1. Classify task: feature or bugfix.
   If unclear, ask whether the behavior existed and is now broken, or is being introduced now.
2. Define the next observable behavior.
3. Design the smallest useful failing test.
4. Decide collaborator strategy at boundaries.
5. Make the test fail for the right reason.
6. Implement the smallest green change.
7. Re-run relevant tests.
8. Refactor only on green.
9. Repeat until behavior is complete.

## Output expectations

While working, explain the current TDD stage clearly:

- `Red`: what behavior is being captured
- `Green`: what minimal code is being added
- `Refactor`: what is being simplified safely

## Guardrails

- Never skip the failing test stage.
- Never start with broad implementation when one failing test would do.
- Never refactor while tests are red.
- Prefer several tiny TDD cycles over one large cycle.
