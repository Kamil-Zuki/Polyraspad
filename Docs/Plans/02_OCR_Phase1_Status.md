# OCR Phase 1 — status

**Goal:** Scanned PDF import → OCR → extracted-text Reader; fix glued words in pdf.js text layer.

## Done

- [x] pdf.js spacing (`pdf-text-content.ts`) + Vitest
- [x] `extract-document-text`: language, pages, UsedOcr, page-break-safe normalize, OCR page limit 40, gRPC deadline 15m
- [x] Media `Put/Get/DeleteDocumentExtract` + book `readingMode` / `hasExtractedText`
- [x] Library import probe → OCR → sidecar → `readingMode=extracted`
- [x] Reader opens extracted books as text session

## Manual QA

1. Digital PDF (en/ru): words not glued; overlays still clickable
2. Scanned PDF: import shows «Importing / OCR…»; opens as text reader with tokens
3. Project `sourceLang=ru`: OCR uses Russian EasyOCR model
4. PDF > 40 pages scan: import succeeds with «OCR: first pages only» note
5. Delete extracted book: sidecar removed from MinIO

## Out of scope (Phase 2)

OCR bounding boxes / overlay alignment, EPUB OCR, GPU, background queue.
