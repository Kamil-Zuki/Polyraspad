# Frontend Rules

## General

- Follow existing `polyraspad-frontend` conventions.
- Prefer existing hooks and API clients.
- Keep UI states explicit: loading, empty, error, success.
- Avoid adding landing-page patterns to app tools.

## Reader

- Reader is the primary learning surface.
- Avoid UI text about lemmas.
- Keep word actions close to the selected term.
- Preserve reading flow: selecting, translating, marking known, ignoring, and reviewing should not force navigation away from reader.

## Visual Statuses

- `NEW`: blue.
- `LINGQ` / `LEARNING`: yellow.
- `KNOWN`: normal white/reading text.
- `IGNORED`: muted and not counted.
