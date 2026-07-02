# Reader + Library + Shadowing MVP

**Product area:** Core learning experience  
**Scope:** Frontend (Next.js), Aggregator + Vocabulary + Media backend support  
**Languages:** en, ru, ko  
**Target materials:** books (.epub, .pdf, .txt) and plain-text articles  
**Last updated:** 2026-07-01

---

## 1. Why this focus

The strongest evidence-based language-learning loop is:

1. **Comprehensible input** — read real content slightly above your level.
2. **Noticing + mining** — click unknown words/phrases and save them with context.
3. **Spaced repetition** — review mined cards at optimal intervals.
4. **Output/shadowing** — repeat sentences aloud to build phonology and fluency.

Polyraspad already has the pieces. This MVP makes the loop usable and visually coherent.

---

## 2. User Flow

### 2.1 First-time flow

```
Landing → Sign up → Choose project / study language pair 
→ /library → Import or open a book → /reader
```

### 2.2 Daily loop

```
Dashboard (optional) 
→ /study/[deckId] (SRS, 10–15 min) 
→ /library → Continue reading (20–30 min) 
→ /shadowing (pronunciation, 5–10 min)
```

### 2.3 Mining flow

```
/reader → select word or drag phrase 
→ popover shows translation + TTS 
→ "Create card" opens /editor with sentence pre-filled 
→ save → word turns yellow (SAVED) and enters SRS
```

### 2.4 Shadowing flow

```
/study card → "Practice pronunciation" 
→ /shadowing?cardId=... 
→ listen to TTS → record yourself → listen to both → rate → next
```

Also reachable from reader via sentence context menu: "Shadow this sentence".

---

## 3. Page Map & Priority

| # | Page | Priority | Owner | Purpose |
|---|------|----------|-------|---------|
| 1 | `/library` | P0 | Frontend | Browse books, see progress, continue reading |
| 2 | `/reader` | P0 | Frontend | Read books, mine words/phrases |
| 3 | `/shadowing` | P0 | Frontend | Pronunciation practice on real sentences |
| 4 | `/editor` | P1 | Frontend | Refine mined cards |
| 5 | `/study/[deckId]` | P1 | Frontend | SRS; add link to shadowing |
| 6 | `/dashboard` | P2 | Frontend | Motivation / streak |

**Out of MVP scope:** Marketplace, Billing, Author profiles, AI agent chat, automatic pronunciation scoring, new languages.

---

## 4. `/library` — Book Library

### 4.1 Layout

- Header: title "Library", import button, view toggle (grid/list), search.
- Top row: "Continue reading" card for the most recently opened book.
- Main grid: book cards with cover, title, progress bar, last-read date.
- Sidebar or chips: collections/filters (All, Reading, Finished, Unread).

### 4.2 Book card data

- Cover image (thumbnail from MediaService or placeholder).
- Title.
- Progress % and last read page.
- Source type icon (epub / pdf / txt).
- CTA: "Continue" or "Start".

### 4.3 Import

Supported formats for MVP:

| Format | Reader mode | Notes |
|--------|-------------|-------|
| `.epub` | paginated text | primary target |
| `.pdf` | original + OCR text layer | fix scaling |
| `.txt` | plain text | easy win |
| pasted text | plain text | no file needed |

Articles (URL fetch) — deferred unless trivial to add.

---

## 5. `/reader` — Redesigned Reader

### 5.1 Current problems

- Page is 3237 lines and mixes library, import, and reading UI.
- PDF original page is small and does not scale.
- OCR text layer is misaligned or hard to read.
- Library inside reader does not feel like a real library.

### 5.2 Target layout

**Desktop — two-pane:**

```
+----------------------------------+
|  Toolbar: book, page, zoom, theme |
+----------------------------------+
|  Original page    |  Extracted   |
|  (scalable)       |  text        |
|                   |  (color-coded|
|                   |   statuses)  |
+----------------------------------+
```

**Mobile — toggle:**

```
[Original] [Text]
```

### 5.3 PDF improvements

- Render page at high DPI / device pixel ratio.
- Add zoom slider (50%–300%).
- Position text layer exactly over canvas using PDF transform.
- Click on text layer word → select token in extracted text.

### 5.4 EPUB improvements

- Render spine items as paginated chapters.
- Consistent font/theme settings.
- Word-level tokenization same as plain text.

### 5.5 Popover actions

- Play TTS for word.
- Show translation (translator integration) and transcription (dictionary).
- Save / Known / Ignore.
- "More details & card" → /editor.
- "Shadow this sentence" → /shadowing.

---

## 6. `/shadowing` — Pronunciation Practice

### 6.1 Goal

Build muscle memory and phonology by repeating real sentences after a native voice.

### 6.2 Input

- `cardId` (study card with sentence) or
- `sentence` + `bookId` (raw sentence from reader).

### 6.3 Loop

```
1. Listen (TTS)
2. Record yourself
3. Listen to your recording
4. Listen to both
5. Self-rate: Bad / Okay / Good
6. Next sentence
```

### 6.4 Controls

- Play/pause TTS.
- Record button with countdown (optional 3-2-1).
- Playback of user recording.
- Difficulty rating buttons.
- "Next" / "Skip" / "Done".

### 6.5 Persistence

Each attempt is saved with:

- card id / sentence text
- TTS audio URL
- user recording blob URL (uploaded to MediaService)
- self-rating
- timestamp

This lets the user review history and see improvement.

---

## 7. Backend Support

### 7.1 MediaService / Aggregator

- Enrich `GET /api/Media/library/{projectId}` with progress and cover.
- Add endpoint to save `lastReadPage` / reading progress.
- Verify `POST /api/Media/generate-audio` supports per-language voices.

### 7.2 VocabularyService

- Add `ShadowingAttempt` entity or store attempts as card note fields.
- Expose:
  - `POST /api/Cards/{cardId}/shadowing-attempts`
  - `GET /api/Cards/{cardId}/shadowing-attempts`

### 7.3 Data model (tentative)

```
ShadowingAttempt
  Id: uuid
  CardId: uuid
  UserId: uuid
  SentenceText: string
  TtsAudioUrl: string
  UserRecordingUrl: string
  SelfRating: int  // 1-3
  CreatedAt: datetime
```

If adding an entity is too heavy for MVP, store attempts as a JSON note field on the card and migrate later.

---

## 8. Metrics (post-MVP)

- Books opened per week.
- Words mined per reading session.
- Shadowing attempts per week.
- SRS retention rate.

---

## 9. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Refactoring 3237-line reader introduces regressions | Split incrementally; keep LingQ regression tests green |
| PDF rendering still flaky after redesign | Use pdf.js best practices; test with multiple PDFs |
| Web Audio recording not supported everywhere | Target Chrome/Edge; show graceful fallback |
| Shadowing feels isolated | Link tightly to cards and reader sentences |

---

## 10. Related Plans & Documents

- `.cursor/plans/active/reader-library-shadowing-mvp.plan.md`
- `.cursor/tasks/active/reader-library-shadowing-mvp/`
- `AGENTS.md` — project conventions
