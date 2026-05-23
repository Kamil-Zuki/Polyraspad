---
name: editor-preview-dashboard-vocab-sidebar
overview: Fix card editor preview, dashboard Progress metrics, remove demo import vocabulary, and repair collapsed sidebar icon rail.
todos:
  - id: image-helper
    content: Add resolveCardImagePreview() helper and fix hydrator + editor-form image gates
    status: completed
  - id: card-preview-ui
    content: Restructure CardPreview — image on top, labeled back sections, remove template gaps
    status: completed
  - id: card-preview-tests
    content: Add card-preview.test.tsx for UUID image and structured back layout
    status: completed
  - id: backend-mapper
    content: Fix AutoMapper TotalLemmas→TotalTerms (+ ProjectStats) with unit test
    status: completed
  - id: vocabulary-stats-ui
    content: Fix distribution chart denominator, Total Terms display, add Distribution helper text
    status: completed
  - id: dashboard-formatting
    content: Round avg daily reviews; fix retention rate divide-by-zero
    status: completed
  - id: heatmap-tooltips
    content: Anki-style hover tooltip with local date + review count; fix UTC date keys
    status: completed
  - id: restore-terms-controller
    content: Restore/create Aggregator TermsController for /api/terms before adding purge endpoint
    status: completed
  - id: restore-vocabulary-page
    content: Restore/create frontend /vocabulary page before adding demo cleanup action
    status: completed
  - id: purge-demo-vocab
    content: Remove only confirmed demo import cards/terms via backend purge endpoint + vocabulary UI action; disable demo import job
    status: completed
  - id: sidebar-collapsed-icons
    content: Fix collapsed sidebar icon rail — reliable icons (Lucide or preloaded FA), overflow, tests for Import
    status: completed
  - id: verify-all
    content: Run dotnet test + npm test for affected modules
    status: completed
isProject: false
---

# Editor Preview + Dashboard + Demo Vocab + Sidebar

## Scope

Four UX areas:

1. **Editor `/editor`** — Card Preview missing image + unstructured back layout
2. **Dashboard `/dashboard#progress`** — vocabulary counts, distribution, avg reviews, heatmap tooltips
3. **Vocabulary `/vocabulary`** — remove demo import terms like `демо-memory-25`
4. **Sidebar collapse** — icon-only rail must show all nav icons (including Import)

---

## Part A — Card Editor Preview

### Root causes

| Issue                  | Cause                                                                                        | File                                                                                             |
| ---------------------- | -------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Missing image (edit)   | Hydrator sets `imageId` then clears `fieldValues.Image` via `setCardState({ imageUrl: "" })` | [`editor-card-hydrator.tsx`](polyraspad-frontend/src/components/editor/editor-card-hydrator.tsx) |
| Missing image (create) | Preview gated on `{{Image}}` in template; default is `{{Expression}}` only                   | [`card-preview.tsx`](polyraspad-frontend/src/components/editor/card-preview.tsx)                 |
| Form dropzone empty    | Checks `imageUrl.trim()` only, ignores `imageId`                                             | [`editor-form.tsx`](polyraspad-frontend/src/components/editor/editor-form.tsx)                   |
| Unstructured back      | Raw template HTML with empty `\n\n` gaps                                                     | [`card-preview.tsx`](polyraspad-frontend/src/components/editor/card-preview.tsx)                 |
| Front gap              | `justify-center` leaves dead space when image missing                                        | same                                                                                             |

### Fix

1. Add `resolveCardImagePreview()` in [`media-preview-url.ts`](polyraspad-frontend/src/lib/utils/media-preview-url.ts) (UUID split like [`card-view-modal.tsx`](polyraspad-frontend/src/components/browser/card-view-modal.tsx)).
2. Fix hydrator — set `imageId` without clearing `Image` field.
3. Restructure `CardPreview`:
   - **Front:** image top, expression below (StudyCard layout)
   - **Back:** labeled rows via new `sentenceMiningEditorBackSections()` in [`sentence-mining-display.ts`](polyraspad-frontend/src/lib/editor/sentence-mining-display.ts)
   - Show image when `previewSrc` exists (no template gate)
4. Fix editor-form dropzone to use `hasImage` from helper.
5. Add `card-preview.test.tsx`.

---

## Part B — Dashboard Progress Statistics

### Root causes

| Issue               | Cause                                                            | File                                                                                                            |
| ------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| Total Terms = 0     | gRPC `TotalLemmas` not mapped to REST `TotalTerms`               | [`AutoMappingProfile.cs`](AggregatorService/AutoMapperProfiles/AutoMappingProfile.cs)                           |
| Distribution empty  | `(count / totalTerms) * 100` when `totalTerms === 0` → NaN       | [`vocabulary-stats.tsx`](polyraspad-frontend/src/components/analytics/vocabulary-stats.tsx)                     |
| Avg reviews decimal | Raw float, no formatting                                         | [`dashboard-progress-section.tsx`](polyraspad-frontend/src/components/dashboard/dashboard-progress-section.tsx) |
| Heatmap hover weak  | Native `title` on 12px cells; UTC date keys may mismatch backend | [`enhanced-heatmap.tsx`](polyraspad-frontend/src/components/analytics/enhanced-heatmap.tsx)                     |

**Distribution meaning:** share of card-linked vocabulary by SRS status (Mature / Learning / New). Add subtitle under heading.

### Fix

1. AutoMapper: `TotalLemmas → TotalTerms`, `MatureLemmas → KnownTerms` for `ProjectStats`.
2. Frontend: `chartTotal = totalTerms || mature+learning+new`; guard bar heights.
3. Format avg reviews with `.toFixed(1)`.
4. Custom heatmap tooltip: formatted date + `"N reviews"` (Anki-style); local `YYYY-MM-DD` keys.
5. Tests for mapper, vocabulary-stats bars, heatmap tooltip.

---

## Part C — Remove Demo Vocabulary

### Verification notes before implementation

Two pieces referenced by the existing routes/tests are currently missing from the checkout and must be restored or created before the cleanup UI can work:

- [`AggregatorService/Controllers/TermsController.cs`](AggregatorService/Controllers/TermsController.cs) is missing, even though [`TermsControllerTests.cs`](AggregatorService.Tests/TermsControllerTests.cs) expects `GET /api/terms` and frontend constants point to `/api/terms`.
- [`polyraspad-frontend/src/app/vocabulary/page.tsx`](polyraspad-frontend/src/app/vocabulary/page.tsx) is missing, even though the sidebar links to `/vocabulary` and the browser currently shows that route in use.

### Source of demo data

Demo terms are created by **Automation IMPORT job** in [`AutomationController.cs`](AggregatorService/Controllers/AutomationController.cs):

```csharp
["Translation"] = new() { StringValue = $"демо-{w}-{i + 1}" },
["Expression"] = new() { StringValue = $"[Import demo #{i + 1}] Practice the word \"{w}\" in context today." },
```

Triggered from study deck page **"Run Import Job"** button in [`study/[deckId]/page.tsx`](polyraspad-frontend/src/app/study/[deckId]/page.tsx) (`runAutomatedImport`, 25 cards).

These cards create `ProjectTerm` + `UserTermStatus` entries shown on `/vocabulary` with meaning `демо-memory-25` etc.

### Fix

**1. Stop creating demo data (prevent recurrence)**

- Remove or hide **"Run Import Job"** demo button from study deck overview (keep real `/import` link).
- Gate `AutomationController` IMPORT job behind `Development`/config flag, or remove demo lexeme generator entirely.

**2. Purge existing demo data**

Add backend cleanup in VocabularyService:

- New method `PurgeDemoImportDataAsync(projectId, userId)` in [`TermService.cs`](VocabularyService/Services/TermService.cs):
  - Identify demo cards conservatively by **card note fields**, not by word text:
    - `Expression` starts with `[Import demo #`
    - `Translation` starts with `демо-`
  - Delete only linked cards that match those demo markers.
  - Remove matching `UserTermStatus` rows only when their meaning/context matches the same demo markers.
  - Remove orphaned `ProjectTerm` rows only after no cards/status rows reference them.
  - Run everything transactionally and return counts: cards deleted, statuses deleted, terms deleted.
- Restore/create Aggregator [`TermsController.cs`](AggregatorService/Controllers/TermsController.cs):
  - Existing `GET /api/terms` contract from [`TermsControllerTests.cs`](AggregatorService.Tests/TermsControllerTests.cs)
  - New `POST /api/terms/purge-demo-import?projectId=...`
- Add gRPC contract + mapper methods for purge response.

**3. Vocabulary UI**

Restore/create [`/vocabulary` page](polyraspad-frontend/src/app/vocabulary/page.tsx) first:

- Use existing `ListProjectTermsResponseDto` / `ProjectTermListItemDto` types from [`types.ts`](polyraspad-frontend/src/lib/api/types.ts).
- Keep current visible behavior from the browser screenshot: status/type/search filters, table rows, and pagination/cursor support if available.
- Add **"Remove demo import data"** action (confirm dialog)
- Calls purge endpoint, invalidates term list query
- Show toast with counts removed

**4. Optional one-time DB patch**

For local dev DBs: optional SQL in `docker/postgres/patches/` using the same conservative markers (`[Import demo #` + `демо-`). Document it; do not auto-run a broad cleanup.

**5. Tests**

- TermService test: cards with both demo markers are purged; real terms like `memory` are kept
- Aggregator test: `GET /api/terms` still passes and purge endpoint maps to gRPC
- Frontend test: `/vocabulary` renders list and cleanup button, then invalidates terms query after successful purge

---

## Part D — Sidebar Collapsed Icon Rail

### Symptom

When sidebar is collapsed, user expects icon-only navigation (Dashboard, Decks, Browser, Vocabulary, Create Card, Books, **Import**, Marketplace) but icons are not visible.

### Likely causes

1. **Font Awesome loads async** via [`font-awesome-loader.tsx`](polyraspad-frontend/src/components/font-awesome-loader.tsx) (client `useEffect`). Collapsed mode shows **icons only** — if FA CSS hasn't loaded, `<i class="fas fa-...">` renders as empty invisible buttons. Expanded mode still has text labels, so nav remains usable.
2. Possible **overflow clipping** on nav scroll container (`overflow-x-hidden`).
3. Collapsed tests don't assert **Import** link visibility ([`sidebar.test.tsx`](polyraspad-frontend/src/components/sidebar.test.tsx)).

Current collapsed layout in [`sidebar.tsx`](polyraspad-frontend/src/components/sidebar.tsx) is structurally correct (`h-11 w-11` icon buttons, 88px rail width in [`app-layout.tsx`](polyraspad-frontend/src/components/layout/app-layout.tsx)).

### Fix

**Option A (preferred): Replace FA with Lucide icons in sidebar**

- Sidebar already has icon names mapped; swap to Lucide components (`Home`, `Layers`, `Search`, `BookOpen`, `PlusCircle`, `BookMarked`, `FileUp`, `Store`) — same approach as study page buttons.
- Icons render immediately without CDN dependency.

**Option B (fallback): Preload Font Awesome**

- Add static `<link rel="stylesheet">` for FA in [`layout.tsx`](polyraspad-frontend/src/app/layout.tsx) `<head>` (not client-only effect).

**Additional polish**

- Collapsed nav links: `shrink-0`, subtle `border border-white/10 bg-white/5` so empty/icon-less state is obvious during load
- Remove `overflow-x-hidden` if it clips icon buttons; keep vertical scroll
- Compact logo header when collapsed (reduce vertical space so more icons fit above fold)
- Expand test: all 8 nav links visible when `isCollapsed={true}`, including Import (`/import`)

---

## Verification checklist

- [ ] Editor preview shows image for UUID and URL sources
- [ ] Editor back preview uses labeled sections
- [ ] Dashboard Total Terms and Distribution bars correct
- [ ] Avg daily reviews shows `2.2` not long float
- [ ] Heatmap hover shows date + review count
- [ ] Demo terms (`демо-*`) purged from vocabulary; demo import disabled
- [ ] Collapsed sidebar shows all nav icons including Import
- [ ] `dotnet test` + `npm test` pass
