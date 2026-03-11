---
name: 11-tdd-integration-verifier
description: Verifies user-visible or cross-layer behavior after narrow TDD checks are green. Use when autonomous Detroit TDD needs browser, integration, or end-to-end proof beyond unit or service tests.
---

# TDD Integration Verifier

## When to use

Use when the change affects user-visible behavior, cross-service flow, or another boundary that narrow tests do not fully prove.

Typical entry point:
- from `09-tdd-validation`

## Responsibilities

1. Choose the smallest broader verification that proves the behavior end to end.
2. Execute that verification.
3. Report whether the user-visible flow is verified or whether another TDD increment is needed.

## Output

Produce a concise handoff with:

- flow or boundary verified
- check performed
- observed outcome
- next route: `09-tdd-validation` or `10-tdd-failure-recovery`

## Guardrails

- Do not replace narrow tests with broad checks.
- Do not run larger verification than necessary.
- If broader verification uncovers a defect, route it back into TDD instead of patching ad hoc.
