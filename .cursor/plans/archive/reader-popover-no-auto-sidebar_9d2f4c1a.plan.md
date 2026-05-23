---
name: reader-popover-no-auto-sidebar
overview: Restore Reader behavior so clicking a word opens only the popup, while the right inspector sidebar opens only by explicit user action.
todos:
  - id: lock-popup-vs-sidebar-behavior
    content: Keep word click behavior popup-only and preserve manual sidebar open controls.
    status: completed
  - id: implement-frontend-fix
    content: Remove automatic sidebar opening on token or phrase click and keep existing sidebar entry points.
    status: completed
  - id: verify-reader-tests
    content: Run focused reader frontend tests and confirm no regressions.
    status: completed
  - id: reviewer-check
    content: Run reviewer pass for regressions in Reader interaction flow.
    status: completed
isProject: false
---

# Reader Popup Without Auto Sidebar

## Goal
When the user clicks a word in Reader, only the popup should appear. The right inspector sidebar should open only when the user explicitly asks for details.

## Out of Scope
- Changes to term status API behavior (`save`, `known`, `ignore`)
- Layout redesign of Reader inspector
- Study/review queue behavior

## Agents
- `product-agent`: not needed
- `backend-agent`: not needed
- `frontend-agent`: implement behavior fix and adjust tests
- `reviewer-agent`: verify interaction regressions

## Contracts To Lock
- Word click => popup visible
- Sidebar remains closed unless opened from explicit controls (toolbar inspector button or popup "More details & card")
- Existing save/known/ignore flow remains intact

## Tasks
- `.cursor/tasks/archive/reader-popover-no-auto-sidebar/frontend.md`
- `.cursor/tasks/archive/reader-popover-no-auto-sidebar/review.md`

## Verification
- `npm test -- reader --run` => passed (`24/24`)
- Reviewer-agent: no blocking issues, GO for archive

## Cleanup
- [x] Move task folder to `.cursor/tasks/archive/reader-popover-no-auto-sidebar/`
- [x] Move plan file to `.cursor/plans/archive/reader-popover-no-auto-sidebar_9d2f4c1a.plan.md`
