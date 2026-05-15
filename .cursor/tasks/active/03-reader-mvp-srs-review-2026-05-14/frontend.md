# Frontend — 03 SRS Review

## Done

- Reader: link to `/study/{inboxDeckId}/session?returnTo=/reader` with Inbox resolved from deck tree; optional due/learning/new summary via `GET /api/Decks/{id}`.
- Study session: `returnTo` query sanitized (`sanitizeInternalReturnPath`); exit × and completion screen support **Continue reading**.
- Study presenter: non-YouTube `sourceMeta` (e.g. `web`) maps to `article` so title/URL display on `StudyCard`.
- Study controls: 2×2 rating grid on small viewports.

## Tests

- `study-session-presenter.test.ts`, `deck-tree-utils.test.ts`, `safe-return-path.test.ts`, existing `reader/page.test.tsx` (deck-queries mocked).
