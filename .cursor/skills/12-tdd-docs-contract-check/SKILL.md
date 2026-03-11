---
name: 12-tdd-docs-contract-check
description: Checks whether autonomous Detroit TDD changes remain aligned with the repository `Docs/` source of truth for APIs, entities, DTOs, gRPC, and navigation. Use when code changes affect contracts, data models, or information architecture.
---

# TDD Docs Contract Check

## When to use

Use when the current increment changes or depends on:

- REST or gRPC contracts
- entities or DTOs
- navigation or information architecture
- other behavior whose source of truth is under `Docs/`

Typical entry point:
- from `09-tdd-validation`

## Responsibilities

1. Identify the affected contract surface.
2. Compare the change against the relevant `Docs/` material.
3. Report whether code is aligned, docs need updating, or behavior assumptions are unclear.

## Output

Produce a concise handoff with:

- impacted contract surface
- relevant `Docs/` area consulted
- alignment result
- next route: `09-tdd-validation`, `10-tdd-failure-recovery`, or escalation

## Guardrails

- Do not invent contracts that conflict with `Docs/`.
- Do not skip docs alignment for API, entity, or navigation changes.
- Escalate when the source of truth is ambiguous or contradictory.
