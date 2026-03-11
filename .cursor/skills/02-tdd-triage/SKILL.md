---
name: 02-tdd-triage
description: Classifies autonomous Detroit TDD work into feature or bugfix, defines done criteria, chooses the highest useful public boundary, and selects the next smallest increment. Use when triaging TDD work or deciding what the next cycle should cover.
---

# TDD Triage

## When to use

Use at the start of autonomous Detroit TDD work and whenever validation failure needs to be turned into the next bounded increment.

## Inputs

Start from:

- the user request or current failure
- current repository state
- any known acceptance criteria or constraints

## Responsibilities

1. Classify the increment as feature or bugfix.
2. Define done criteria for this increment.
3. Choose the highest useful public boundary.
4. Select the next smallest observable behavior.
5. Identify whether broader integration or docs alignment will likely be needed later.

## Output

Produce a concise handoff with:

- classification: `feature` or `bugfix`
- next behavior to drive
- chosen public boundary
- done criteria for this increment
- immediate next route

## Next route

- `feature` -> `03-detroit-tdd-feature`
- `bugfix` -> `04-regression-tdd-bugfix`

## Guardrails

- Do not write tests or production code here.
- Do not plan multiple increments when one next behavior will do.
- Do not choose a lower-level boundary unless the higher-level one is impractical.
