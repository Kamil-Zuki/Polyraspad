# Frontend — Reader Inspector UX

Plan ID: `reader-inspector-ux-fixes`
Status: done
Owner: `frontend-agent`

## Files changed
- `polyraspad-frontend/src/app/reader/page.tsx` — URL hydration/sync, open inspector on word select
- `polyraspad-frontend/src/components/reader/reader-inspector-layout.tsx` — Close, height, single mobile/desktop branch
- `polyraspad-frontend/src/app/reader/reader-inspector-panel.tsx` — Save term copy
- `polyraspad-frontend/src/components/reader/reader-word-popover.tsx` — Save term label
- `polyraspad-frontend/src/app/reader/page.test.tsx` — navigation mock, updated selectors

## Verification
- `npm test -- reader --run` — 24 passed
