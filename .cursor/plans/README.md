# Cursor Lead Plans

`.cursor/plans/` stores temporary coordination plans created by `lead-agent`.

## Where Plans Live

- Active plans: `.cursor/plans/active/<plan-id>.md`
- Completed plans are not archived here. Delete them after all related tasks are done.
- Stable architecture or product decisions must be promoted to `context/decisions/`, `context/plans/`, or `Docs/` before deleting the temporary plan.

## Plan ID

Use a short kebab-case ID:

```text
reader-phrase-lingq-2026-05-13
```

## Plan Template

```markdown
# <Plan Title>

Plan ID: `<plan-id>`
Status: active
Created: YYYY-MM-DD
Owner: `lead-agent`

## Goal
<What should change for the user or system>

## Out of Scope
- <What is explicitly not part of this plan>

## Agents
- `product-agent`: <responsibility or "not needed">
- `backend-agent`: <responsibility or "not needed">
- `frontend-agent`: <responsibility or "not needed">
- `reviewer-agent`: <responsibility or "not needed">

## Contracts To Lock
- <REST/gRPC DTO/API client/UI state/data assumption>

## Tasks
- `.cursor/tasks/active/<plan-id>/product.md`
- `.cursor/tasks/active/<plan-id>/backend.md`
- `.cursor/tasks/active/<plan-id>/frontend.md`
- `.cursor/tasks/active/<plan-id>/review.md`

## Verification
- <test/build/check>

## Cleanup
- [ ] All task files deleted
- [ ] Temporary plan deleted
- [ ] Durable decisions promoted if needed
```

## Cleanup Rule

When all task files for a plan are complete:

1. Verify the feature or plan outcome.
2. Promote durable decisions to `context/` or `Docs/` if needed.
3. Delete `.cursor/tasks/active/<plan-id>/`.
4. Delete `.cursor/plans/active/<plan-id>.md`.
