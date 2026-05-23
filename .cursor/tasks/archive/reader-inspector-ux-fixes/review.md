# Review — Reader Inspector UX

Plan ID: `reader-inspector-ux-fixes`
Status: done
Owner: `reviewer-agent`

## Result
- Save vs Create card copy clarified (`Save term`, helper text in inspector and popover).
- URL hydration + sync implemented in `page.tsx` with race guard during library load.
- Desktop Close added to inspector header; single render path via `matchMedia` (no duplicate DOM).
- Sidebar uses `max-h-[calc(100dvh-5rem)]` + inner scroll.

## Verification
- `npm test -- reader --run` — 24/24 passed.

## Residual risks
- Text-only paste sessions (no `bookId`) still reset on reload — only library books restore via URL.
- `npm install` was required locally for missing `jszip` (dependency already in package.json).
