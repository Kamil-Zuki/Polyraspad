---
name: 01-tdd-orchestrator
description: Dispatches autonomous Detroit TDD across explicit subagent roles for triage, strategy, red, green, refactor, validation, recovery, integration verification, and docs checks. Use when the user asks for Detroit TDD, regression-first TDD, red-green-refactor, or autonomous TDD execution. Always prefer the numbered versions of skills (e.g., 06-tdd-red) over unnumbered versions.
---

# TDD Orchestrator

## When to use

Use when the task should be driven through explicit Detroit TDD stages instead of ad hoc implementation.

Canonical launch phrase:
`Run autonomous Detroit TDD for <task>. Keep going until verified.`

If the user wants full autonomous delivery until completion, pair this skill with `00-autonomous-development`.

## Responsibility

This skill is the Detroit TDD dispatcher. It decides which stage skill should act next and what output that stage must hand back.

## Routing map (Delegation via Worker)

Skills are not subagents. To execute these stages, the Orchestrator MUST launch a `worker` subagent and instruct it to read the corresponding skill file.

- `02-tdd-triage` -> Assign to worker. Task: classify the task, choose boundary, define the next increment.
- `03-detroit-tdd-feature` -> Assign to worker. Task: feature playbook when the next increment is new behavior.
- `04-regression-tdd-bugfix` -> Assign to worker. Task: bugfix playbook when the next increment is broken behavior.
- `05-tdd-test-strategy` -> Assign to worker. Task: design one test, its assertions, and doubles policy.
- `06-tdd-red` -> Assign to worker. Task: add the failing test and prove red.
- `07-tdd-green` -> Assign to worker. Task: implement the smallest passing change.
- `08-refactor-on-green` -> Assign to worker. Task: clean up only after green.
- `09-tdd-validation` -> Assign to worker. Task: run the next validation gate and decide whether scope can advance.
- `10-tdd-failure-recovery` -> Assign to worker. Task: turn failed validation into the next bounded task.
- `11-tdd-integration-verifier` -> Assign to worker. Task: verify user-visible or cross-layer behavior.
- `12-tdd-docs-contract-check` -> Assign to worker. Task: align API, entity, and navigation changes with `Docs/`.

## Architectural Gate
After the `07-tdd-green` or `08-refactor-on-green` stage is successful, you MUST launch the `architect` subagent (`subagent_type: architect`) to verify layer integrity before continuing.

## Default flow

1. Start with `02-tdd-triage`.
2. Route to `03-detroit-tdd-feature` or `04-regression-tdd-bugfix`.
3. Route to `05-tdd-test-strategy`.
4. Route to `06-tdd-red`.
5. Route to `07-tdd-green`.
6. Route to `08-refactor-on-green` only if cleanup is justified and tests are green.
7. Route to `09-tdd-validation`.
8. If validation fails, route to `10-tdd-failure-recovery` and loop back to the smallest necessary stage.
9. If broader verification is needed, route to `11-tdd-integration-verifier`.
10. If contracts or navigation changed, route to `12-tdd-docs-contract-check`.

## Required handoff data

Each stage should hand back the smallest useful artifact for the next stage:

- `02-tdd-triage` -> classification, boundary, done criteria, next behavior
- playbook skill -> policy constraints for the chosen increment
- `05-tdd-test-strategy` -> one concrete test design
- `06-tdd-red` -> failing test name and observed failure
- `07-tdd-green` -> minimal change summary and green target
- `08-refactor-on-green` -> simplification summary
- `09-tdd-validation` -> pass/fail decision and next route

## Guardrails

- Never skip the failing test stage.
- Never start with broad implementation when one failing test would do.
- Never refactor while tests are red.
- Prefer several tiny TDD cycles over one large cycle.
- Route failed validation into recovery instead of open-ended debugging.
- Ask for clarification if feature-vs-bugfix classification changes the approach.
