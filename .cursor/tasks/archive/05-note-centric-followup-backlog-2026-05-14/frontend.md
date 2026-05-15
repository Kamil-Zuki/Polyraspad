# frontend-agent Task

Plan ID: `05-note-centric-followup-backlog-2026-05-14`
Agent: `frontend-agent`
Status: done
Can run in parallel: no

## Objective
Editor parity with capture extension field set; API type for optional capture deck.

## Deliverables
- `editor-form.tsx`: show Source title + Source URL in main mining fields; merge `sourceMeta` only when those fields are empty in `fieldValues`.
- `types.ts`: optional `deckId` on `CaptureCardDto`.

## Verification
- `npx tsc --noEmit`

## Handoff
Completed in lead run 2026-05-14.
