# Polyraspad Frontend — Руководство для разработчика

**Цель:** помочь разработчику, знакомому с Next.js, быстро разобраться в архитектуре фронтенда Polyraspad.

**Версия:** Next.js 16, React 19, TypeScript strict, Tailwind CSS v4, App Router.

---

## 1. Что это за проект

Polyraspad — платформа для изучения языков по методу LingQ: читаешь реальные книги/статьи, кликаешь незнакомые слова, сохраняешь их в карточки, повторяешь через SRS (spaced repetition). Фронтенд — это Next.js 16 приложение, которое работает как BFF (Backend-for-Frontend): часть API-роутов выполняется на сервере Node.js и проксирует запросы к .NET backend (AggregatorService).

---

## 2. Технологии (коротко)

| Технология | Зачем |
|------------|-------|
| **Next.js 16** | Фреймворк. App Router — файлы в `app/` = страницы. API routes в `app/api/` = серверные обработчики. |
| **React 19** | UI. Server Components по умолчанию, `'use client'` только для интерактивности. |
| **TypeScript strict** | Типы. Запрещён `any` (ESLint ругается). |
| **Tailwind CSS v4** | Стили. CSS-first через `@import "tailwindcss"` в `globals.css`. Нет CSS-модулей. |
| **TanStack Query** | Загрузка данных с сервера, кэширование, инвалидация. |
| **Radix UI + lucide-react** | Базовые accessible компоненты (диалоги, поповеры) + иконки. |
| **framer-motion** | Анимации (реже используется). |

---

## 3. Структура папок

```
polyraspad-frontend/src/
├── app/                          # App Router — страницы и API-роуты
│   ├── page.tsx                  # Главная (редирект на проекты/дашборд)
│   ├── layout.tsx                # Корневой layout: провайдеры, шрифты, глобальные стили
│   ├── (auth)/                   # Группа маршрутов — auth
│   ├── api/                      # BFF API-роуты (серверные)
│   │   ├── ai/                   # AI-прокси: generate, models, mining-draft
│   │   └── ollama/               # Локальные LLM (устарело)
│   ├── auth/                     # Страница входа/регистрации
│   ├── browser/                  # Обзор карточек (каталог)
│   ├── dashboard/                # Главный дашборд
│   ├── decks/                    # Дерево колод (папки + колоды)
│   ├── editor/                   # Редактор карточки (sentence mining)
│   ├── generator/                # Генератор карточек (AI)
│   ├── import/                   # Импорт материалов
│   ├── library/                  # Библиотека книг (раньше было внутри reader)
│   ├── marketplace/              # Маркетплейс колод
│   ├── profile/                  # Профиль пользователя
│   ├── projects/                 # Список проектов (языковых пар)
│   ├── reader/                   # Читалка (PDF/EPUB/текст + майнинг)
│   ├── settings/                 # Настройки
│   ├── study/                    # SRS-очередь (Anki-like)
│   ├── subscriptions/            # Подписки на колоды
│   └── vocabulary/               # Словарь (список терминов)
│
├── components/                   # React-компоненты
│   ├── auth/                     # Формы входа, регистрации
│   ├── billing/                  # UI оплаты
│   ├── browser/                  # Карточки в обзоре
│   ├── dashboard/                # Виджеты дашборда
│   ├── decks/                    # Карточки колод, диалоги
│   ├── editor/                   # Поля редактора, AI-ассистент
│   ├── layout/                   # Шапка, сайдбар, обёртки
│   ├── library/                  # Компоненты библиотеки (сайдбар коллекций, сетка книг)
│   ├── marketplace/              # Карточки продуктов
│   ├── profile/                  # Аватар, настройки профиля
│   ├── projects/                 # Список проектов
│   ├── reader/                   # Читалка: popover, inspector, viewport PDF
│   ├── settings/                 # Формы настроек
│   ├── sidebar/                  # Боковое меню навигации
│   ├── study/                    # Карточка изучения, кнопки оценки
│   └── ui/                       # Переиспользуемые UI-элементы (кнопки, инпуты, диалоги)
│
├── contexts/                     # React Context (глобальное состояние)
│   ├── auth-context.tsx          # Авторизация, токен, пользователь
│   ├── editor-card-context.tsx   # Состояние редактора карточки
│   ├── editor-language-context.tsx # Язык редактора
│   └── project-context.tsx       # Текущий проект (языковая пара en→ru)
│
├── lib/                          # Утилиты, API-клиенты, бизнес-логика
│   ├── api/                      # HTTP-клиенты к Aggregator
│   │   ├── base-api-client.ts    # Базовый fetch с Bearer, retry, обработка 401
│   │   ├── types.ts              # DTO-типы (сгенерированные/ручные)
│   │   ├── media-client.ts       # Загрузка файлов, аудио, библиотека
│   │   └── integration-client.ts # Переводчик, словарь
│   ├── agent/                    # AI-агент (чат, тул-регистр)
│   ├── decks/                    # Утилиты дерева колод
│   ├── editor/                   # Логика редактора: sentence mining, шаблоны
│   ├── integrations/             # Настройки интеграций (переводчик, словарь)
│   ├── languages/                # Языковые коды, валидация пар
│   ├── navigation/               # Хелперы навигации
│   ├── polyguide/                # AI-ассистент PolyGuide
│   ├── react-query/              # Query keys, хуки TanStack Query
│   ├── server/                   # **Сервер-only** код (BFF)
│   │   ├── aggregator-ai-proxy.ts  # Прокси к Aggregator AI
│   │   ├── editor-ai-provider.ts   # Выбор AI-провайдера (Gemini / Aggregator)
│   │   └── gemini-generate.ts      # Прямой вызов Gemini API
│   ├── studio/                   # Редактор карточек (legacy naming)
│   └── utils/                    # Утилиты: cn(), media preview, парсинг CSV
│
├── test/                         # Vitest setup, моки
└── assets/                       # Статика (шрифты, изображения)
```

---

## 4. Ключевые паттерны

### 4.1 Server Component по умолчанию

Файлы в `app/` — **Server Components** (SC). Они не имеют доступа к браузерным API, не могут использовать `useState`, `useEffect`. Для интерактивности добавляй `'use client'` в начале файла.

```tsx
// app/reader/page.tsx — 'use client' потому что использует useState, useEffect
"use client"
import { useState } from "react"
```

**Когда `'use client'` нужен:**
- `useState`, `useEffect`, `useRef`
- Обработчики событий (`onClick`, `onSubmit`)
- Доступ к `window`, `localStorage`, `document`
- React Context Consumer

**Когда НЕ нужен:**
- Просто рендеришь данные, полученные через props
- Делаешь `fetch` на сервере (в SC можно async/await напрямую!)

### 4.2 BFF — API Routes

Файлы `app/api/.../route.ts` — это серверные обработчики, которые выполняются на Node.js. Они проксируют запросы к AggregatorService (.NET backend).

```
app/api/ai/generate/route.ts    → POST → Aggregator /api/ai/generate
app/api/ai/models/route.ts      → GET  → Aggregator /api/ai/models
```

**Зачем BFF?**
- Скрыть секретные ключи (API ключи AI не попадают в браузер)
- Добавить shared secret (`X-Ai-Proxy-Key`)
- Агрегировать запросы к нескольким backend-сервисам

### 4.3 TanStack Query — загрузка данных

Вместо `useEffect` + `fetch` используем TanStack Query (React Query).

```tsx
// lib/react-query/queries.ts
export function useDeckTree(projectId: string) {
  return useQuery({
    queryKey: ["decks", "tree", projectId],
    queryFn: () => apiClient.decks.getTree(projectId),
    enabled: !!projectId,
  })
}

// На странице
const { data: deckTree, isLoading } = useDeckTree(projectId)
```

**Query Key** — это массив, который идентифицирует данные. При инвалидации TanStack Query перезагружает все запросы с этим ключом:

```tsx
queryClient.invalidateQueries({ queryKey: ["decks", "tree", projectId] })
```

### 4.4 Context — глобальное состояние

```tsx
// contexts/project-context.tsx
const ProjectContext = createContext<ProjectContextValue | null>(null)

export function ProjectProvider({ children }) {
  const [currentProject, setCurrentProject] = useState<Project | null>(null)
  // ... загрузка из localStorage / API
  return (
    <ProjectContext.Provider value={{ currentProject, setCurrentProject }}>
      {children}
    </ProjectContext.Provider>
  )
}

// Использование
const { currentProject } = useProjectContext()
```

**Провайдеры монтируются в `app/layout.tsx`** — они оборачивают всё приложение.

### 4.5 API Client — как фронтенд говорит с бэкендом

```
Browser → Next.js BFF (/api/ai/generate) → AggregatorService (.NET) → downstream gRPC services
       → или напрямую Aggregator REST (/api/Projects, /api/Decks, ...)
```

**Base API Client** (`lib/api/base-api-client.ts`):
- Добавляет `Authorization: Bearer <JWT>`
- Обрабатывает 401 → редирект на `/auth`
- Retry на сетевые ошибки

**URL backend-а:** `NEXT_PUBLIC_API_URL` (env var, обычно `http://localhost:5000` в dev).

---

## 5. Поток данных — пример

### Открытие книги в Reader

```
1. Пользователь кликает "Read" на /library
   → router.push(`/reader?bookId=${book.id}`)

2. /reader/page.tsx (Client Component)
   → useEffect видит ?bookId в URL
   → вызывает openBookMutation.mutate(book)
   → загружает PDF/EPUB через fetchDocumentBytes()
   → рендерит страницу через pdf.js

3. Пользователь кликает слово
   → handleTokenClick → setMinedWord({ word, sentence, tokenIndex })
   → появляется ReaderWordPopover с переводом + TTS
   → "More details & card" → router.push('/editor')

4. /editor/page.tsx
   → pre-filled поля: sentence, word, translation
   → пользователь дополняет → Save
   → POST /api/Cards → карточка создана
   → слово в reader становится SAVED (жёлтое)
```

---

## 6. Важные файлы для понимания

| Файл | Что внутри |
|------|------------|
| `src/app/layout.tsx` | Корневой layout: провайдеры (Auth, Project, Query, Toast), шрифты, глобальные стили |
| `src/lib/constants.ts` | API_ENDPOINTS, ROUTES — все пути в одном месте |
| `src/lib/api/base-api-client.ts` | HTTP-клиент: Bearer, retry, 401 handling |
| `src/lib/api/types.ts` | TypeScript-типы DTO (CardDto, DeckDto, TermDto, ...) |
| `src/contexts/auth-context.tsx` | Авторизация: JWT, login, logout, refresh token |
| `src/contexts/project-context.tsx` | Текущий проект: языковая пара (en→ru), Inbox deck |
| `src/app/reader/page.tsx` | Читалка — самый сложный компонент (~2200 строк) |
| `src/app/study/[deckId]/page.tsx` | SRS-очередь — карточки на повторение |
| `src/app/editor/page.tsx` | Редактор карточки (sentence mining) |
| `src/app/library/page.tsx` | Библиотека книг |

---

## 7. Env-переменные

| Переменная | Зачем |
|------------|-------|
| `NEXT_PUBLIC_API_URL` | URL AggregatorService (`http://localhost:5000`) |
| `NEXT_PUBLIC_APP_URL` | URL фронтенда (`http://localhost:3000`) |
| `AI_PROXY_API_KEY` | Shared secret для BFF-прокси к Aggregator AI |
| `GEMINI_API_KEY` | Альтернативный AI-провайдер (Gemini) |
| `EDITOR_AI_PROVIDER` | `aggregator` или `gemini` |

**Важно:** `NEXT_PUBLIC_*` попадают в клиентский бандл. Секреты (API ключи) — только в серверные `.env` или BFF-роуты.

---

## 8. Как добавить новую страницу

```tsx
// 1. Создать файл
// src/app/mypage/page.tsx

export default function MyPage() {
  return <div>Hello</div>
}

// 2. Добавить в ROUTES (если нужна навигация)
// src/lib/constants.ts
MY_PAGE: "/mypage"

// 3. Добавить в sidebar (если нужна навигация)
// src/components/sidebar.tsx
```

---

## 9. Как добавить API-роут (BFF)

```ts
// src/app/api/myfeature/route.ts
import { NextRequest, NextResponse } from "next/server"

export async function POST(request: NextRequest) {
  const body = await request.json()
  
  // Прокси к Aggregator
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/myfeature`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": request.headers.get("authorization") ?? "",
    },
    body: JSON.stringify(body),
  })
  
  const data = await res.json()
  return NextResponse.json(data, { status: res.status })
}
```

---

## 10. Тесты

- **Фреймворк:** Vitest + jsdom + React Testing Library
- **Запуск:** `npm test -- --run` (CI) или `npm test` (watch mode)
- **Где:** рядом с тестируемым файлом (`page.test.tsx` рядом с `page.tsx`)
- **Mock:** TanStack Query, router, fetch — через `vi.mock()`

---

## 11. Частые вопросы

**Q: Почему `app/api/` — серверные, а `app/page.tsx` — клиентские?**
A: `route.ts` всегда серверный. `page.tsx` — Server Component по умолчанию, но добавляешь `'use client'` и он становится клиентским.

**Q: Куда класть общие компоненты?**
A: `src/components/ui/` — кнопки, инпуты, диалоги. `src/components/` — фича-компоненты (reader, study, editor).

**Q: Как работает авторизация?**
A: JWT в `localStorage`. Base API Client добавляет `Authorization: Bearer <token>` к каждому запросу. При 401 — редирект на `/auth`.

**Q: Где хранится состояние?**
A: Глобальное — Context (auth, project). Серверное — TanStack Query. Локальное — `useState` в компоненте.

**Q: Как добавить новый язык?**
A: Сейчас поддерживаются en/ru/ko. Добавить язык — нужно обновить: `study-language-preferences.ts`, `reader-reading-themes.ts`, backend (VocabularyService), и NLP (inclusive Python service).

---

## 12. Полезные команды

```bash
cd polyraspad-frontend

# Dev server
npm run dev          # localhost:3000

# Build
npm run build        # Production build

# Tests
npm test             # Watch mode
npm test -- --run    # CI mode (один прогон)

# Type check
npm run typecheck    # tsc --noEmit
```

---

## 13. Связь с backend

```
Browser
  │
  ▼
polyraspad-frontend (Next.js :3000)
  │  BFF routes: /api/ai/*
  │  Direct API calls: /api/Projects, /api/Decks, ...
  ▼
AggregatorService (.NET :5000)
  │  gRPC → VocabularyService, AgentService, MediaService, BillingService, auth-module
  ▼
Postgres / Redis / MinIO / Python inclusive
```

---

## 14. Deep Dive: Study (SRS-очередь)

Study — это Anki-like интерфейс для повторения карточек. Пользователь открывает колоду (`/study/[deckId]`), нажимает "Start Study Session", и попадает в сессию (`/study/[deckId]/session`).

### 14.1 Архитектура Study

```
/study/[deckId]/page.tsx          — Обзор колоды (статистика, кнопка старта)
/study/[deckId]/session/page.tsx  — Сессия повторения (карточка + кнопки)
/components/study/study-card.tsx  — UI карточки (front/back)
/components/study/study-controls.tsx — Кнопки оценки (Again/Hard/Good/Easy)
/components/study/study-session-presenter.ts — Маппинг DTO → ViewModel
/lib/api/study-client.ts          — HTTP-клиент для Study API
```

### 14.2 Поток сессии

```
1. Пользователь нажимает "Start Study Session"
   → router.push(`/study/${deckId}/session`)

2. StudySessionPage монтируется
   → useEffect запускает сессию:
     a. Опционально: получает экспериментальную вариант (A/B тест)
     b. POST /api/study/session → { projectId, deckId, mode: "STANDARD" }
     c. Получает StudySessionDto { id, queueStats: { new, review, learning } }
     d. GET /api/study/session/{id}/next → CardStudyDto

3. Пользователь видит карточку (Front)
   → Предложение с выделенным словом
   → Клик или Space → reveal (показать Back)

4. Пользователь оценивает (1-4)
   → POST /api/study/session/{id}/review
     { cardId, rating, durationMs }
   → Получает ReviewResponseDto { nextReviewDate, interval, isLeech }
   → GET /api/study/session/{id}/next → следующая карточка

5. Когда карточки закончились (204 No Content)
   → sessionComplete = true
   → Показывается экран завершения (streak, daily progress)
   → Инвалидируются кэши: userSettings, dailySummary, deck
```

### 14.3 Типы данных

```ts
// lib/api/types.ts
interface StudySessionDto {
  id: string;
  projectId: string;
  status: "ACTIVE" | "COMPLETED";
  startTime: string;
  cardsReviewed: number;
  queueStats: QueueStatsDto;     // { new, review, learning }
}

interface CardStudyDto {
  id: string;
  type: "SENTENCE_MINING";
  content: {
    note: NotePayloadDto;
    targetIndex: { start: number; len: number }; // позиция слова в предложении
  };
  sourceMeta?: SourceMetaDto;      // youtube | book | article
  media?: CardMediaDto;            // imageUrl, audioUrl
  srsState: SrsStateDto;           // { state, currentInterval, step, dueUtc }
  nextIntervals: Record<number, string>; // интервалы FSRS для каждого рейтинга
  siblingsCount: number;
}

interface ReviewCardRequestDto {
  cardId: string;
  rating: 1 | 2 | 3 | 4;          // 1=Again, 2=Hard, 3=Good, 4=Easy
  durationMs: number;            // время на раздумие
}
```

### 14.4 StudyCard — рендеринг карточки

`StudyCard` — чистый презентационный компонент. Получает все данные через props:

```ts
interface StudyCardProps {
  sentence: string;                // Предложение (Expression)
  targetWord: string;              // Слово для изучения
  highlightRange?: { start, len }; // Точная позиция в предложении
  backSections: SentenceMiningStudySection[]; // Поля: Word, Translation, Definition...
  sourceType?: "youtube" | "book" | "article";
  sourceTitle?: string;
  sourceTimestamp?: string;        // HH:MM:SS
  sourceUrl?: string;              // Ссылка на источник
  imageSrc?: string;
  imageFallbackSrc?: string;
  audioSrc?: string;               // Аудио (после reveal)
  srsState?: { state: string; currentInterval: number };
  isRevealed: boolean;
  onReveal: () => void;
}
```

**Front** — только предложение с выделенным словом. Пользователь кликает или жмёт Space.

**Back** — предложение + все заполненные поля (Word, Translation, Definition...) + аудио. SRS-бейдж показывает состояние карточки (New/Learning/Review/Relearning).

**Highlight** — использует `highlightRange` (UTF-16 индексы от бэкенда), а не `split()` — это важно для юникода и составных слов.

### 14.5 StudyControls — кнопки оценки

```ts
interface StudyControlsProps {
  onRate: (rating: 1 | 2 | 3 | 4) => void;
  isRevealed: boolean;
  onReveal: () => void;
  onUndo?: () => void;
  canUndo?: boolean;
  intervals?: Record<number, string>; // FSRS интервалы
}
```

**Клавиатура:**
- Space / Enter — reveal (когда скрыто)
- 1 — Again
- 2 — Hard
- 3 — Good
- 4 — Easy
- Ctrl+Z — Undo (только после reveal)

**Игнорирование горячих клавиш:** Если фокус в `input`, `textarea` или `contentEditable` — горячие клавиши не срабатывают. Это предотвращает случайные оценки при редактировании.

### 14.6 FSRS интервалы

Бэкенд (VocabularyService + Python inclusive) считает интервалы по алгоритму FSRS. Фронтенд получает их в `nextIntervals`:

```ts
// Пример nextIntervals
{
  1: "10m",    // Again — через 10 минут
  2: "2d",     // Hard — через 2 дня
  3: "4d",     // Good — через 4 дня
  4: "14d"     // Easy — через 14 дней
}
```

`formatStudyInterval()` нормализует интервалы в компактный вид: `10m`, `2d`, `3w`, `6mo`, `2y`.

### 14.7 Undo (отмена последней оценки)

```ts
const handleUndo = async () => {
  await apiClient.study.undoReview(session.id);
  setSession(prev => ({ ...prev, cardsReviewed: prev.cardsReviewed - 1 }));
  await fetchNextCard(session.id); // Возвращает предыдущую карточку
};
```

Ограничение: нельзя отменить, если `cardsReviewed === 0` или идёт загрузка следующей карточки.

### 14.8 Leech detection

Если карточка слишком часто помечается "Again" — бэкенд помечает её как `isLeech: true`. Фронтенд показывает уведомление: "This card was marked as a leech and suspended." Карточка приостанавливается (не показывается в сессии), пока пользователь не исправит её в редакторе.

### 14.9 Study Session Presenter

`study-session-presenter.ts` — чистая функция маппинга `CardStudyDto → StudyCardProps`:

```ts
export function toStudyCardViewModel(card: CardStudyDto): StudyCardViewModel {
  const { content, srsState } = card;
  const fv = content.note?.fieldValues;
  
  // 1. Извлекаем предложение из поля Expression
  const sentence = noteFieldPlainString(fv, SENTENCE_MINING.Expression);
  
  // 2. Извлекаем слово из targetIndex (точная позиция в предложении)
  const targetWord = sentence.slice(content.targetIndex.start, 
    content.targetIndex.start + content.targetIndex.len);
  
  // 3. Собираем back sections (Word, Translation, Definition...)
  const backSections = sentenceMiningStudyBackSections(fv);
  
  // 4. Маппим sourceMeta → sourceType (youtube/book/article)
  const sourceType = mapStudySourceType(card.sourceMeta);
  
  // 5. Резолвим image URL (media service или прямая ссылка)
  const imageSrc = getPreviewImageSrc({ imageId, imageUrl, apiBaseUrl });
  
  return { sentence, targetWord, highlightRange, backSections, ... };
}
```

Это **Presenter pattern** — отделение логики преобразования данных от UI.

---

*Документ создан: 2026-07-03. Обновляй при изменении архитектуры.*
