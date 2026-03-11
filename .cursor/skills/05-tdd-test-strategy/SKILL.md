---
name: 05-tdd-test-strategy
description: Designs one concrete TDD test increment, including scenario scope, assertion focus, public boundary, and doubles policy. Use when choosing the next failing test, naming it, or deciding what should stay real versus mocked.
---

# TDD Test Strategy

## When to use

Use after feature or bugfix triage policy is clear and before writing the next failing test.

## Responsibilities

Design exactly one concrete test increment:

- choose the scenario
- choose the public boundary
- define the expected observable outcome
- name the test clearly
- choose real collaborators versus doubles

## Defaults

- Prefer one test for one behavior.
- Prefer the highest useful public boundary.
- Prefer state, output, and user-visible assertions over interaction-only checks.
- Use real domain objects by default.
- Replace only unstable or expensive external boundaries such as database, HTTP, queues, file system, clock, or random.

## Output

Produce a concise handoff with:

- test name
- boundary under test
- observable assertions
- required fixtures or doubles
- why this is the smallest useful failing test

Then route to `06-tdd-red`.

## Guardrails

- Do not design a suite when one next test is enough.
- Do not mirror implementation structure with unnecessary mocks.
- Do not assert private implementation details unless they are contractual.
