# Library Information Architecture (Content-First)

**Цель:** Переход от deck-first к content-first модели как в LingQ

## Текущая проблема

Сейчас в навигации два раздела:
- **Library** (`/reader`) - книги и PDF
- **Decks** (`/library`) - колоды карточек

Пользователь не видит единого потока "контент → изучение → повторение".

## Целевая IA

```
📚 Library (единый раздел)
├── 📖 Continue Reading
│   ├── Последняя открытая книга (PDF/page)
│   ├── Последний текст
│   └── Быстрый доступ
│
├── 📑 My Content
│   ├── Книги (PDF)
│   ├── Тексты
│   ├── Коллекции
│   └── Импорт
│
├── 📊 Study
│   ├── Колоды (legacy SRS)
│   ├── Review Queue
│   └── Statistics
│
└── 🏪 Marketplace
    ├── Публичные колоды
    └── Подписки
```

## Навигация

### Новая структура sidebar

```
[📊 Dashboard]      → Overview, streak, daily goals
[📚 Library]         → Content-first experience
[🎓 Study]          → SRS, review, decks
[👤 Profile]        → Settings, stats
```

### Library Section Details

```
Library (/library) - редирект с /reader
├── Header: "Library" + [Import Content] button
│
├── Section: "Continue Reading"
│   ├── Card: Book cover + title + "Page 42 of 156" + [Continue]
│   ├── Card: Last text + progress bar + [Continue]
│   └── Empty state: "Start reading something new!"
│
├── Section: "My Books"
│   ├── Grid: Book covers with progress indicators
│   ├── Sort: Recent | Progress | Title
│   └── Filter: All | In Progress | Completed
│
├── Section: "Collections"
│   ├── List: Collection cards with book counts
│   └── [Create Collection]
│
├── Section: "Texts"
│   ├── List: Saved texts with word counts
│   └── [Paste New Text]
│
└── Footer: Import options (PDF, URL, YouTube)
```

## Card Component Spec

### Book Card

```
┌─────────────────────────────┐
│  ┌───────────────────────┐  │
│  │                       │  │
│  │    [Book Cover]       │  │
│  │    or PDF thumbnail   │  │
│  │                       │  │
│  └───────────────────────┘  │
│  The Great Gatsby           │
│  📄 156 pages               │
│  ━━━━━━╺━━━━━━━━ 45%        │
│  42 known · 23 learning     │
│  [Continue] [⋮]             │
└─────────────────────────────┘
```

### Progress Indicator

- **Visual:** Progress bar + percentage
- **Data:** `knownCount / totalUniqueWords`
- **Tooltip:** "You know 45% of unique words in this text"

### Term Stats Badge

```
┌─────────────────────┐
│ 📊 42 known         │
│ 🟡 23 learning      │
│ 🔵 15 new           │
└─────────────────────┘
```

## Import Flow

```
[Library Header]
    ↓
[+] Import Content
    ↓
┌──────────────────────────────┐
│  Choose import source:       │
│  📁 Upload PDF/EPUB          │
│  📝 Paste Text               │
│  🔗 Import from URL          │
│  📺 YouTube (with captions)   │
└──────────────────────────────┘
    ↓
[Processing] → [Analyze] → [Save to Library] → [Open in Reader]
```

## Data Model

### ReaderLibraryBook (extended)

```typescript
interface ReaderLibraryBook {
  id: string;
  title: string;
  author?: string;
  language: string;
  
  // Progress
  lastPageNumber: number;
  totalPages: number;
  readPercentage: number;
  
  // Stats (computed from terms)
  totalUniqueWords: number;
  knownWords: number;
  learningWords: number;
  newWords: number;
  
  // Metadata
  coverImageUrl?: string;
  addedAt: Date;
  lastOpenedAt: Date;
  completedAt?: Date;
  
  // Relations
  projectId: string;
  collectionIds: string[];
}
```

### ReadingSession

```typescript
interface ReadingSession {
  id: string;
  userId: string;
  bookId: string;
  
  // Position
  currentPage: number;
  currentTextOffset?: number; // for plain text
  
  // Session stats
  wordsMarked: number;
  cardsCreated: number;
  timeSpent: number; // seconds
  
  // Timestamps
  startedAt: Date;
  lastActivityAt: Date;
  endedAt?: Date;
}
```

## Continue Reading Algorithm

```
GetContinueReadingItem(userId):
  1. Найти активную сессию (lastActivityAt < 24h)
     - Если есть: вернуть эту книгу/текст
  
  2. Найти последнюю открытую книгу
     - Если lastPageNumber < totalPages: вернуть её
  
  3. Найти текст с самым высоким newWords count
     - Если newWords > 10: предложить продолжить
  
  4. Показать welcome empty state с suggestions
```

## Search & Filter

```
Search Library:
  - By title/author (fuzzy search)
  - By language
  - By progress (not started | in progress | completed)
  - By content type (book | text | collection)

Sort:
  - Recently opened
  - Progress (high → low)
  - Title (A → Z)
  - Added date
  - Difficulty (new words %)
```

## Миграция с текущей модели

### Phase 1: Объединение навигации
- [ ] Убрать разделение Library/Decks в sidebar
- [ ] Сделать единый `/library` entry point
- [ ] Добавить "Continue Reading" на dashboard

### Phase 2: Данные
- [ ] Добавить `lastPageNumber` в frontend flow
- [ ] Создать `ReadingSession` tracking
- [ ] Добавить term stats per book

### Phase 3: UX улучшения
- [ ] Book cards с progress
- [ ] Collection management
- [ ] Import improvements

## Acceptance Criteria

- [ ] Единый вход в Library с dashboard
- [ ] "Continue Reading" всегда виден если есть активность
- [ ] Book cards показывают real progress (не placeholder)
- [ ] Import flow ведет сразу в reader
- [ ] Decks доступны через Study subsection, не конфликтуют с content
