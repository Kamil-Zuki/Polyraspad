# Agents: Order and Groups

Numbered by **logical importance** in the Detroit TDD flow. When invoking via the Task tool, use the `name` from the frontmatter (without numbers).

---

## 00 — Orchestration

| #  | File | name | Role |
|----|------|------|------|
| 00 | `detroit-tdd-orchestrator.md` | `detroit-tdd-orchestrator` | Coordinator: composes tasks in `.cursor/tasks/`, delegates to workers, manages TDD stages, and launches architect / infra-verifier when necessary. |

**When to invoke:** Implementing a feature/module, **fixing bugs/regressions**, or running autonomous TDD until verified complete.

---

## 01 — Execution

| #  | File | name | Role |
|----|------|------|------|
| 01 | `worker.md` | `worker` | Executor: performs red/green/refactor for a given task, writes code and tests, returns a structured report. |

**When to invoke:** Delegating a specific task (stage) from the orchestrator.

---

## 02 — Validation / Review

| #  | File | name | Role |
|----|------|------|------|
| 02 | `architect.md` | `architect` | Reviewer (read-only): verifies architectural layers, ensures no transport bleeding, checks transactions and contracts. Runs after green/refactor. |

**When to invoke:** After a worker completes a stage — architectural review before advancing to the next increment.

---

## 03 — Infrastructure / Recovery

| #  | File | name | Role |
|----|------|------|------|
| 03 | `infra-verifier.md` | `infra-verifier` | Diagnoses Docker, RabbitMQ, Redis, and ports; resolves Testcontainers and integration test failures. |

**When to invoke:** Environment or integration dependency failures; the orchestrator delegates here before a worker retries.

---

## Flow Order

1. **00** — Orchestrator receives the goal, classifies it (feature or bugfix), and composes tasks.
2. **01** — Worker executes the specific assigned TDD stage (e.g., *only* red, or *only* green) and returns the result.
3. **02** — Architect reviews the code after green/refactor.
4. **03** — On infrastructure failures, the orchestrator launches infra-verifier, then delegates back to 01.

Names for invocation in code/commands remain unchanged: `detroit-tdd-orchestrator`, `worker`, `architect`, `infra-verifier`.