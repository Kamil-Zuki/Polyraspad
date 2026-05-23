---
name: reader-inspector-ux-fixes
overview: Fix Reader inspector UX — clarify Save vs Create card, restore reading session on reload via URL, add desktop Close for the right sidebar, and fix clipped sidebar layout in reading mode.
todos:
  - id: product-save-copy
    content: Clarify Save = saved term (yellow highlight) vs Create card = SRS deck in inspector copy.
    status: completed
  - id: url-hydration
    content: Hydrate bookId and collectionId from URL query params on /reader load and reopen the same book.
    status: completed
  - id: url-sync
    content: Sync URL when opening a book or entering reading mode (router.replace with projectId and bookId).
    status: completed
  - id: desktop-close
    content: Add Close control to desktop inspector header that hides the entire sidebar column.
    status: completed
  - id: sidebar-clipping
    content: Fix sidebar height and scroll clipping so Meaning, deck, and AI sections are fully reachable.
    status: completed
  - id: review-verify
    content: Run reader frontend tests and manually verify reload, Close, and sidebar scroll behavior.
    status: completed
isProject: false
---

# Reader Inspector UX Fixes

## Goal
Fix Reader inspector UX: explain Save clearly in UI, restore reading session on reload via URL, add desktop Close for the right sidebar, and fix clipped sidebar layout in reading mode.

## Out of Scope
- Backend term API changes (Save contract already correct)
- Full resizable split panel (`ReaderResizableSplit` integration)
- Study queue "review count vs empty session" bug (separate plan)

## Agents
- `product-agent`: Save vs Create card copy and close/collapse behavior
- `backend-agent`: not needed
- `frontend-agent`: URL hydration, sidebar layout, Close button
- `reviewer-agent`: after implementation

## Contracts To Lock
- Save → `POST /api/terms` with `status: "SAVED"` (yellow LEARNING highlight)
- Reader deep link: `/reader?projectId=&bookId=` must reopen the same book
- Desktop Close hides inspector column; Collapse only hides panel body

## Tasks
- `.cursor/tasks/archive/reader-inspector-ux-fixes/product.md`
- `.cursor/tasks/archive/reader-inspector-ux-fixes/frontend.md`
- `.cursor/tasks/archive/reader-inspector-ux-fixes/review.md`

## Verification
- `npm test -- reader --run` — 24 passed
- Manual: open book → reload → same book opens (via `bookId` URL)
- Manual: Close hides right sidebar on desktop; Collapse still works
- Manual: inspector scrolls fully (Meaning, deck, AI sections visible)

## Cleanup
- [x] Task folder → `.cursor/tasks/archive/reader-inspector-ux-fixes/`
- [x] Plan → `.cursor/plans/archive/reader-inspector-ux-fixes_4c8e2a1f.plan.md`
- [x] All frontmatter todos — `completed`
