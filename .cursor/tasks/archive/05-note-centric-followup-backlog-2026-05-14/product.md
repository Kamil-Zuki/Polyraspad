# product-agent Task

Plan ID: `05-note-centric-followup-backlog-2026-05-14`
Agent: `product-agent`
Status: done
Can run in parallel: no

## Objective
Lock P2 product decision: optional target deck on capture vs Inbox-only.

## Decision
- **Default:** unchanged — capture without `deckId` continues to use project **Inbox**.
- **Optional:** clients may pass `deckId` for a deck owned by the user in the same project; invalid id → clear error (no silent fallback to Inbox).

## Handoff
Encoded in REST/gRPC contracts and VocabularyService behavior 2026-05-14.
