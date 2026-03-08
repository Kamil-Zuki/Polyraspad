---
name: tdd-orchestrator
description: Routes and enforces TDD-specific execution across feature work, bugfixes, test design, test doubles, and refactoring. Use when the user asks to implement via TDD, Detroit TDD, regression-first TDD, or wants explicit red-green-refactor execution.
---

# TDD Orchestrator

## When to use

Use when the user wants explicit TDD execution: Detroit TDD, regression-first TDD, or step-by-step red-green-refactor.

If the user wants full autonomous delivery until completion, pair this skill with `autonomous-development`.

## Routing

- **New feature / new behavior** -> use `detroit-tdd-feature`
- **Bug / regression / defect** -> use `regression-tdd-bugfix`
- **Need help designing tests** -> use `tdd-test-design`
- **Need to decide mocks/fakes/real objects** -> use `test-doubles-boundaries`
- **Code is green and needs cleanup** -> use `refactor-on-green`
- **Ambiguous task** -> first determine whether expected behavior is new or previously broken

## TDD flow

1. Classify task: feature or bugfix.
2. Define the next observable behavior.
3. Design the smallest useful failing test.
4. Decide boundary strategy and test doubles only as needed.
5. Drive `Red -> Green -> Refactor`.
6. Repeat in small cycles until the requested behavior is complete.

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
- Ask for clarification if feature-vs-bugfix classification changes the approach.
