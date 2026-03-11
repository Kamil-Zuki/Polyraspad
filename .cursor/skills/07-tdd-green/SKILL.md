---
name: 07-tdd-green
description: Implements the minimum production change needed to make the current failing TDD test pass. Use when running the green stage after a valid red test has been established.
---

# TDD Green

## When to use

Use only after `06-tdd-red` has produced a valid failing test.

## Responsibilities

1. Make the smallest production change needed for the current test.
2. Re-run the narrowest relevant test scope.
3. Stop as soon as the target behavior turns green.

## Output

Produce a concise handoff with:

- what minimal code changed
- which target test is now green
- whether `08-refactor-on-green` is justified

## Next route

- if cleanup is justified -> `08-refactor-on-green`
- otherwise -> `09-tdd-validation`

## Guardrails

- Do not implement adjacent behaviors in the same step.
- Do not perform speculative refactors here.
- Do not keep coding after the target test is green.
