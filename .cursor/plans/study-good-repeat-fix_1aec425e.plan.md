---
name: study-good-repeat-fix
overview: Fix Study queue migration so reviewed due cards are not reintroduced from stale legacy Redis queues, which makes Good appear stuck at 1 day.
todos:
  - id: fix-legacy-queue-resurrection
    content: Update AnkiStudyQueueService so legacy Redis queues are deleted or migrated once, never reused repeatedly.
    status: completed
  - id: add-study-queue-regression
    content: Add a focused regression test proving stale legacy queues cannot resurrect a reviewed due card.
    status: completed
  - id: verify-study-good-flow
    content: Run backend tests and verify Good no longer repeats the same 1d card.
    status: completed
isProject: false
---

# Fix Study Good Repeats 1 Day

## Root Cause

`AnkiStudyQueueService.InitializeDueQueueAsync` writes the same ordered card IDs to both the new `study:session:{id}:due` list and the legacy `study:session:{id}:queue` list. `PopDueCardIdAsync` pops only from `:due`. When `:due` becomes empty, `MigrateLegacyQueueAsync` copies the still-full legacy `:queue` back into `:due`, so already-reviewed cards are shown again even though their DB `due` is tomorrow.

This matches the DB evidence: the same `card_id` has repeated `Good` reviews seconds apart, while each `due_after` stays at the next day.

## Files

- [VocabularyService/Services/Study/AnkiStudyQueueService.cs](VocabularyService/Services/Study/AnkiStudyQueueService.cs)
- [VocabularyService.Tests/AnkiFsrsStudyRegressionTests.cs](VocabularyService.Tests/AnkiFsrsStudyRegressionTests.cs) or a focused queue-service test file

## Implementation

1. Stop reintroducing stale legacy queues.
   - In `InitializeDueQueueAsync`, delete the legacy `:queue` key instead of mirroring `:due` into it.
   - In `MigrateLegacyQueueAsync`, after a successful one-time migration, delete the legacy key so it cannot be copied again.
   - Optionally de-duplicate migrated values before pushing to `:due`.

2. Add a regression test.
   - Seed a session queue with one due card.
   - Simulate an old/stale legacy `:queue` containing the same card.
   - Pop/review the due card so `:due` becomes empty.
   - Verify the next pop does not resurrect the same card from legacy.

3. Optional runtime cleanup for local dev.
   - Clear stale Redis keys matching `study:session:*:queue` after deploying the fix, or let the code delete them when sessions are initialized/migrated.

## Verification

- Run `dotnet test VocabularyService.Tests --filter "FullyQualifiedName~AnkiFsrsStudyRegressionTests|FullyQualifiedName~StudyQueue"`.
- Start a Study session, press `Good` on a review card, and verify the same card does not immediately reappear unless it is an intentional learning step due now.
- Inspect latest `review_logs`: repeated same-card `Good` rows seconds apart should stop.
