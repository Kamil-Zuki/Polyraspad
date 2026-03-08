---
name: detroit-tdd-feature
description: Drives new feature work with Detroit TDD. Use when implementing a feature, adding behavior, or building a new use case through red-green-refactor.
---

# Detroit TDD Feature Flow

## When to use

Use for new behavior, feature work, and API/UI use cases.
If the task is a defect in existing behavior, route to `regression-tdd-bugfix`.

## Workflow

1. Define the next observable behavior.
2. Write one failing test through the highest useful public boundary.
3. Run the smallest relevant test scope and confirm red.
4. Implement the minimum code to turn green.
5. Refactor only after tests are green.
6. Repeat in small increments.

## Test target

- Prefer controller/service/hook/use-case boundaries.
- Assert outputs, state, and user-visible behavior.
- Avoid testing private methods directly.
- Default to one test for one behavior.

## Constraints

- Do not write production code before a failing test exists.
- Do not solve multiple behaviors in one step.
- Keep each cycle small enough to explain in 1-2 sentences.

## Done

- New behavior is covered by tests.
- Relevant tests pass.
- Duplication introduced during green step is cleaned up.
