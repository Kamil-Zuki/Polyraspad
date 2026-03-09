---
name: tdd-orchestrator
description: Dispatches autonomous Detroit TDD across explicit subagent roles for triage, strategy, red, green, refactor, validation, recovery, integration verification, and docs checks. Use when the user asks for Detroit TDD, regression-first TDD, red-green-refactor, or autonomous TDD execution.
---

# TDD Orchestrator

## When to use

Use when the task should be driven through explicit Detroit TDD stages instead of ad hoc implementation.

Canonical launch phrase:
`Run autonomous Detroit TDD for <task>. Keep going until verified.`

If the user wants full autonomous delivery until completion, pair this skill with `autonomous-development`.

## Responsibility

This skill is the Detroit TDD dispatcher. It decides which stage skill should act next and what output that stage must hand back.

## Routing map

- `tdd-triage` -> classify the task, choose boundary, define the next increment
- `detroit-tdd-feature` -> feature playbook when the next increment is new behavior
- `regression-tdd-bugfix` -> bugfix playbook when the next increment is broken behavior
- `tdd-test-strategy` -> design one test, its assertions, and doubles policy
- `tdd-red` -> add the failing test and prove red
- `tdd-green` -> implement the smallest passing change
- `refactor-on-green` -> clean up only after green
- `tdd-validation` -> run the next validation gate and decide whether scope can advance
- `tdd-failure-recovery` -> turn failed validation into the next bounded task
- `tdd-integration-verifier` -> verify user-visible or cross-layer behavior when narrow tests are insufficient
- `tdd-docs-contract-check` -> align API, entity, and navigation changes with `Docs/`

## Default flow

1. Start with `tdd-triage`.
2. Route to `detroit-tdd-feature` or `regression-tdd-bugfix`.
3. Route to `tdd-test-strategy`.
4. Route to `tdd-red`.
5. Route to `tdd-green`.
6. Route to `refactor-on-green` only if cleanup is justified and tests are green.
7. Route to `tdd-validation`.
8. If validation fails, route to `tdd-failure-recovery` and loop back to the smallest necessary stage.
9. If broader verification is needed, route to `tdd-integration-verifier`.
10. If contracts or navigation changed, route to `tdd-docs-contract-check`.

## Required handoff data

Each stage should hand back the smallest useful artifact for the next stage:

- `tdd-triage` -> classification, boundary, done criteria, next behavior
- playbook skill -> policy constraints for the chosen increment
- `tdd-test-strategy` -> one concrete test design
- `tdd-red` -> failing test name and observed failure
- `tdd-green` -> minimal change summary and green target
- `refactor-on-green` -> simplification summary
- `tdd-validation` -> pass/fail decision and next route

## Guardrails

- Never skip the failing test stage.
- Never start with broad implementation when one failing test would do.
- Never refactor while tests are red.
- Prefer several tiny TDD cycles over one large cycle.
- Route failed validation into recovery instead of open-ended debugging.
- Ask for clarification if feature-vs-bugfix classification changes the approach.
