---
name: test-doubles-boundaries
description: Chooses mocks, stubs, fakes, and real collaborators for TDD. Use when deciding what to mock, isolating external dependencies, or preventing over-mocking in tests.
---

# Test Doubles and Boundaries

## Default policy

Use real domain objects by default. Replace only expensive or unstable external boundaries.

## Mock vs real

- Mock or fake: database, HTTP, message bus, file system, clock, random, external services.
- Prefer real: value objects, mappers, validators, domain rules, pure helpers.
- Avoid mocking internal collaborators just because they exist.

## Decision rule

Ask: "Is this collaborator part of the behavior under test, or only infrastructure?"

- If it is infrastructure, isolate it.
- If it is domain behavior, keep it real.

## In Detroit TDD

- Interaction assertions are secondary.
- Prefer observable result assertions first.
- Use mocks to protect boundaries, not to mirror implementation structure.
