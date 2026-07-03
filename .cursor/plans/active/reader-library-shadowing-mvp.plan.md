---
name: reader-library-shadowing-mvp
overview: Redesign the Reader UX, promote Book Library to a first-class /library page, fix PDF/EPUB original-page scaling and OCR usability, and add a dedicated /shadowing page for pronunciation practice on mined sentences.
todos:
  - id: reader-ux-audit
    content: Audit the current 3237-line /reader page and define split points / component boundaries.
    status: completed
  - id: library-page-extract
    content: Extract the book library UI from /reader into a standalone /library page with progress, covers, and collections.
    status: completed
  - id: reader-redesign
    content: Redesign /reader layout with scalable original page view, readable extracted text, and synced word highlight.
    status: completed
  - id: ocr-pdf-fix
    content: Fix PDF original-page scaling and text-layer positioning so the page is readable and clickable.
    status: completed
  - id: epub-support
    content: Ensure EPUB books render cleanly in the new reader layout.
    status: completed
  - id: shadowing-page
    content: Create /shadowing page with TTS playback, user recording via Web Audio API, and self-rating loop.
    status: pending
  - id: shadowing-integration
    content: Link /shadowing to study cards and reader sentences via query params / shared state.
    status: pending
  - id: tests-regression
    content: Add/update tests for reader, library, and shadowing; ensure LingQ regressions still pass.
    status: pending
isProject: false
---

# Reader + Library + Shadowing MVP

## Goal

Сделать ядро обучения в Polyraspad удобным и научно обоснованным: пользователь заходит в **библиотеку**, открывает **реальную книгу**, **майнит предложения и слова**, а затем отрабатывает **произношение** через shadowing.

**Языки в scope:** en, ru, ko (расширять не нужно).  
**Материалы:** книги (.epub, .pdf с OCR), plain text; статьи — только если не требуют больших модификаций.

## Out of Scope

- Marketplace, Billing, Subscriptions, Author profiles.
- AI-агент / PolyGuide agent в shadowing (только TTS + запись).
- Автоматическая оценка произношения (STT/phoneme comparison) — только self-rating.
- Социальные фичи (шаринг коллекций можно оставить как есть, но не улучшать).
- Расширение языков за en/ru/ko.

## Agents

| Agent | Ответственность |
|-------|-----------------|
| `product-agent` | User flow, acceptance criteria, page priorities |
| `frontend-agent` | Рефакторинг /reader, новый /library, новый /shadowing |
| `backend-agent` | Book metadata + progress, audio endpoint, shadowing persistence |
| `reviewer-agent` | LingQ regressions, accessibility, mobile sanity |

## Tasks

- `.cursor/tasks/active/reader-library-shadowing-mvp/product.md`
- `.cursor/tasks/active/reader-library-shadowing-mvp/frontend.md`
- `.cursor/tasks/active/reader-library-shadowing-mvp/backend.md`
- `.cursor/tasks/active/reader-library-shadowing-mvp/review.md`

## User Flow

### A. Ежедневный вход
```
/login → /projects → /library → открыть книгу → /reader
```

### B. Майнинг из книги
```
/reader → выделить слово/фразу → popover → "Create card" 
→ /editor (sentence + target word pre-filled) → сохранить → вернуться к чтению
```

### C. Shadowing после study
```
/study/[deckId] → карточка с предложением → "Practice pronunciation" 
→ /shadowing?cardId=... → слушаешь TTS → записываешь себя → оцениваешь → следующее
```

### D. Shadowing прямо из reader
```
/reader → контекстное меню предложения → "Shadow this sentence" 
→ /shadowing?sentence=...&bookId=...
```

## Page Priority

| # | Page | Priority | Why |
|---|------|----------|-----|
| 1 | `/library` | P0 | Главная точка входа для материалов |
| 2 | `/reader` | P0 | Ядро чтения и майнинга |
| 3 | `/shadowing` | P0 | Произношение и разговорный скилл |
| 4 | `/editor` | P1 | Уже есть, улучшить pre-fill |
| 5 | `/study/[deckId]` | P1 | Уже есть, добавить CTA на shadowing |
| 6 | `/dashboard` | P2 | Мотивация, можно позже |

## Key Features

### 1. `/library` — Book Library

- Сетка/список книг с обложками.
- Прогресс чтения (%) и последняя открытая страница.
- Коллекции ( drag-and-drop как сейчас или упрощённый вид).
- Фильтры: All, Reading, Finished, Unread.
- Search по названию.
- "Continue reading" сверху.
- Import button: epub / pdf / txt / paste text.

### 2. `/reader` — Redesigned Reader

- **Two-pane layout:**
  - Left/Top: original page (PDF page / EPUB rendered page) — масштабируется, pinch-to-zoom / slider.
  - Right/Bottom: extracted text с цветовой разметкой статусов слов.
- **Mobile:** toggle original ↔ text.
- **PDF fix:**
  - Canvas рендерится в высоком DPI.
  - Text layer позиционируется точно поверх canvas.
  - Клик по слову в text layer выбирает токен.
- **EPUB:** рендеринг глав с пагинацией.
- **Popover:** TTS, translation, Save/Known/Ignore, "Shadow sentence".

### 3. `/shadowing` — Pronunciation Practice

- **Input:** sentence text + source card/book reference.
- **Loop:**
  1. Play TTS (native voice).
  2. Record user (Web Audio API).
  3. Play both.
  4. Self-rate: Bad / Okay / Good.
  5. Next sentence (from same deck/book or random due).
- **Persist:** attempts (audio blob, rating, timestamp) linked to card.

## Contracts To Lock

- `GET /api/Media/library/{projectId}` — enriched book list with progress.
- `POST /api/Media/library/progress` — save last read page.
- `POST /api/Media/generate-audio` — sentence-level TTS (already exists, verify voice per language).
- `POST /api/Cards/{cardId}/shadowing-attempts` — save attempt.
- `GET /api/Cards/{cardId}/shadowing-attempts` — list attempts.

## Verification

```powershell
# Frontend build
cd polyraspad-frontend
npm run build
npm test -- --run

# Backend build
dotnet build AggregatorService/AggregatorService.csproj -c Release
dotnet build VocabularyService/VocabularyService.csproj -c Release

# Manual smoke
# 1. Open /library, see books with progress.
# 2. Open a PDF/EPUB book, zoom original page, click word, create card.
# 3. Go to /shadowing?cardId=..., record audio, rate, save.
```

## Execution Order

1. **product-agent** — finalize acceptance criteria and visual direction.
2. **frontend-agent** — extract `/library`, refactor `/reader`, fix PDF scaling.
3. **backend-agent** — enrich library endpoint, add progress persistence and shadowing attempt contract.
4. **frontend-agent** — build `/shadowing` page and link from study/reader.
5. **reviewer-agent** — regression tests, accessibility, mobile check.

## Cleanup

- [ ] All todos `completed` or `cancelled`
- [ ] Tasks moved to `.cursor/tasks/archive/reader-library-shadowing-mvp/`
- [ ] Plan moved to `.cursor/plans/archive/reader-library-shadowing-mvp.plan.md`
- [ ] Durable decisions promoted to `Docs/Product/reader-library-shadowing-mvp.md`
