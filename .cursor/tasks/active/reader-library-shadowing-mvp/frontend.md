# Frontend Task — Reader-Library-Shadowing MVP

## Goal

Redesign Reader UX, extract Book Library to `/library`, and add a dedicated `/shadowing` page.

## Work items

1. **Reader refactor**
   - Split 3237-line `reader/page.tsx` into subcomponents/modules.
   - Redesign layout: original page (scalable) + extracted text side-by-side or overlay toggle.
   - Fix PDF original page scaling and text-layer alignment.
   - Fix EPUB rendering flow.

2. **Library page**
   - Move book library from `/reader` into `/library`.
   - Grid/list view, search, filters, collections.
   - Show reading progress, last opened, cover thumbnails.
   - "Continue reading" CTA.

3. **Shadowing page**
   - Create `/shadowing` route.
   - Receive sentence + source card via query params or state.
   - TTS playback, user recording via Web Audio API, playback of both.
   - Difficulty self-rating, save attempt, next sentence.

4. **Navigation**
   - Link from study card "Practice pronunciation" → `/shadowing?cardId=...`
   - Link from reader sentence context menu → `/shadowing?sentence=...`

## Acceptance criteria

- [ ] `/library` renders independently and is reachable from main nav.
- [ ] Reader page loads a book and shows scalable original + readable text.
- [ ] Mining flow still works after refactor (regression tests pass).
- [ ] `/shadowing` records audio and saves session.

## References

- Plan: `.cursor/plans/active/reader-library-shadowing-mvp.plan.md`
