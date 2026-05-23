---
name: reader-popover-tts
overview: Add Listen (TTS) to the Reader word popover using the same generateAudio API as the inspector, with inline loading, error state, and auto-playback.
todos:
  - id: extract-tts-helper
    content: Share TTS generation logic between inspector and popover in page.tsx.
    status: completed
  - id: popover-ui
    content: Add Listen button and playback UX to reader-word-popover.tsx.
    status: completed
  - id: reader-tts-test
    content: Add regression test for popover Listen calling generateAudio.
    status: completed
  - id: verify-reader-tests
    content: Run reader frontend tests.
    status: completed
isProject: false
---

# Reader Popover TTS

## Verification
- `npm test -- reader/page.test --run` — 16/16 passed
