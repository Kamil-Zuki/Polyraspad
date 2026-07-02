# Product Task — Reader-Library-Shadowing MVP

## Goal

Define the MVP user flow for reading real books/articles, mining sentences/words, and practicing shadowing. Scope languages to en/ru/ko for now.

## Decisions needed

1. Confirm `/library` becomes the primary entry for books; `/decks` stays for SRS decks.
2. Confirm shadowing is a separate `/shadowing` page reachable from study card and reader sentence.
3. Decide whether articles are in scope for this MVP or deferred to post-MVP.

## Acceptance criteria

- [ ] User can browse a visually clear book library at `/library`.
- [ ] User can open a book in `/reader` with scalable original view and readable extracted text.
- [ ] User can mine a word/phrase from reader into a card with context sentence.
- [ ] User can open `/shadowing` for a saved sentence, listen to TTS, record themselves, and rate the attempt.
- [ ] Shadowing session is linked to the source card/sentence.

## References

- Plan: `.cursor/plans/active/reader-library-shadowing-mvp.plan.md`
- Product doc: `Docs/Product/reader-library-shadowing-mvp.md`
