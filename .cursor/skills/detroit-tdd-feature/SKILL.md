---
name: detroit-tdd-feature
description: Defines the Detroit TDD policy for new behavior and feature increments. Use when triage classifies the next increment as a feature, use case, API addition, or new user-visible behavior.
---

# Detroit TDD Feature Playbook

## When to use

Use only when `tdd-triage` classifies the next increment as new behavior.

If the task is a defect in existing behavior, route to `regression-tdd-bugfix`.

## Entry

Enter from `tdd-triage` with:

- the next smallest behavior
- the highest useful public boundary
- done criteria for this increment

## Policy

- Prefer controller, service, hook, use-case, or component behavior boundaries.
- Start with the smallest happy path that proves the new behavior exists.
- Assert outputs, state, or user-visible behavior.
- Avoid testing private methods directly.
- Keep one increment focused on one behavior.

## Output

Produce a concise feature policy handoff for the next stage:

- what new behavior is being added
- which public boundary should express it
- what should not be included in this increment

Then route to `tdd-test-strategy`.

## Guardrails

- Do not write production code before `tdd-red` proves a failing test.
- Do not mix multiple new behaviors into one increment.
- Do not turn feature work into a broad implementation batch.
