# Frontend Task

Plan ID: `reader-popover-no-auto-sidebar`
Agent: `frontend-agent`
Status: done
Can run in parallel: no

## Objective
Fix Reader interaction so clicking a word/phrase opens only popup and does not auto-open right sidebar.

## Result
- Removed automatic sidebar opening from word and phrase click handlers in `polyraspad-frontend/src/app/reader/page.tsx`.
- Kept explicit sidebar open paths intact:
  - toolbar Inspector button
  - popup "More details & card"
- Updated `polyraspad-frontend/src/app/reader/page.test.tsx` for popover-first flow.

## Verification
- `npm test -- reader --run` => passed (`24/24`).

## Handoff
- Ready for review pass and archive.
