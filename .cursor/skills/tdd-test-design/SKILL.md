---
name: tdd-test-design
description: Designs clear, behavior-focused tests for TDD. Use when naming tests, choosing assertion scope, or deciding which scenarios to cover first.
---

# TDD Test Design

## Start with behavior

Write tests around what the user or caller observes:

- returned value
- state transition
- emitted response
- visible UI behavior
- persisted side effect at a boundary

## Naming

Prefer:

- `should_<outcome>_when_<condition>`
- `returns_<result>_when_<condition>`
- `renders_<state>_when_<condition>`

## Scenario order

1. Smallest happy path.
2. Most important edge case.
3. One failure path.
4. Additional cases only when they add real protection.

## Quality checks

- One test should explain one reason to fail.
- Avoid large fixture setup if a smaller object graph works.
- Keep assertions focused and readable.
- Do not assert implementation details unless they are contractual.
- Prefer the highest useful public boundary before dropping lower.
