# 03 — SRS / Session Review — MVP scope (active)

Plan: `03-reader-mvp-srs-review-2026-05-14`

## Locked MVP behavior

1. **Reader → Session Review**: While a reading session is open, user can open **Session Review** for the project **Inbox** deck (same deck as capture/mining), with optional **due / learning** counts when stats load.
2. **Return path**: Study session accepts `?returnTo=` (internal path only). Exit (×) and session-complete flow offer **Continue reading** when `returnTo=/reader`.
3. **Source on cards**: Study UI shows **source title / URL** for mined cards when `sourceMeta` is present (not only YouTube).
4. **FSRS**: Unchanged — scheduling remains via `inclusive` / backend `StudyService`; no duplicate scheduler in the app.
5. **Mobile Study controls**: Rating buttons use a **2×2 grid** on narrow screens to avoid horizontal overflow.

## Out of scope (plan 04+)

- PWA install, Library IA overhaul, project-wide “study all decks” entry from Reader without picking Inbox.
