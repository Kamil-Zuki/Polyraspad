---
name: tdd-red
description: Creates the next failing test and explicitly proves it is red before production changes. Use when running the red stage of Detroit TDD or regression-first TDD.
---

# TDD Red

## When to use

Use after `tdd-test-strategy` defines the next test increment.

## Responsibilities

1. Add or adjust the next test at the chosen public boundary.
2. Run the narrowest relevant test scope.
3. Confirm the test fails for the intended reason.

## Output

Produce a concise handoff with:

- failing test name
- observed failure
- why the failure proves the desired gap
- next route to `tdd-green`

## If the test does not fail correctly

- tighten the scenario
- fix the test design if it is malformed
- return to `tdd-test-strategy` if the scenario itself is wrong

## Guardrails

- Never edit production code before proving red.
- Never accept a flaky or ambiguous failure as a valid red state.
- Do not broaden scope to make the test fail.
