# Backend Rules

## General

- Keep service boundaries clear.
- Keep REST, gRPC, DTO, and frontend types synchronized.
- Add migrations for schema changes.
- Avoid destructive migrations without an explicit migration plan.

## Vocabulary

- New reader vocabulary features use terms and phrases, not lemmas.
- Existing `ProjectLemma` and `Card.LemmaId` are legacy until removed by an explicit cleanup plan.
- Duplicate checks should use exact normalized term/phrase matching.
- Store enough context for reader-created terms to support review and future contexts.
