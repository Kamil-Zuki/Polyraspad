# Backend Task — Reader-Library-Shadowing MVP

## Goal

Support book library metadata, reader progress, sentence-level audio, and shadowing attempt persistence.

## Work items

1. **MediaService / Aggregator**
   - Ensure `/api/Media/library/{projectId}` returns enriched book metadata (cover, progress %, last read page, source type).
   - Persist `lastReadPage` and `readingProgress` per user+book.
   - Ensure `/api/Media/generate-audio` supports sentence-level text with language and optional voice.

2. **VocabularyService**
   - Add `ShadowingAttempt` entity (optional): card id, audio url, user recording url, rating, created at.
   - Or store shadowing metadata as card media/note fields if entity migration is too heavy for MVP.

3. **Contracts**
   - DTO for book list item with progress.
   - DTO for shadowing session request/response.

## Acceptance criteria

- [ ] Book list endpoint returns progress and cover.
- [ ] Audio generation works for single sentences.
- [ ] Shadowing attempts can be saved and listed per card.

## References

- Plan: `.cursor/plans/active/reader-library-shadowing-mvp.plan.md`
