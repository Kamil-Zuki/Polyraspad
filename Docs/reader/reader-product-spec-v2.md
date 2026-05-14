# Reader Product Spec v2

**Цель:** LingQ-style чтение с непрерывным flow "чтение → LingQ → review"

## User Stories

### US-1: Открытие и чтение текста
Как пользователь, я хочу открыть текст или PDF и видеть подсветку слов по статусам, чтобы читать с пониманием своего прогресса.

**Критерии приемки:**
- [ ] Загрузка PDF или ввод текста запускает анализ
- [ ] Слова подсвечены цветами: синий (NEW), желтый (LINGQ), белый (KNOWN), приглушенный (IGNORED)
- [ ] Фразы подсвечиваются как единое целое с приоритетом над словами
- [ ] Навигация по страницам/главам работает плавно
- [ ] Сохраняется позиция чтения для PDF

### US-2: Работа со словами в контексте
Как пользователь, я хочу кликнуть на слово и сразу сохранить его со значением, не покидая reader.

**Критерии приемки:**
- [ ] Клик по слову открывает инспектор с предложением
- [ ] Автоматический перевод показывается сразу
- [ ] Можно ввести свое значение
- [ ] Кнопки: "Create LingQ", "Known", "Ignore"
- [ ] Проверка дубликатов показывает существующие карточки

### US-3: Работа с фразами
Как пользователь, я хочу выделить несколько соседних слов и сохранить их как фразу.

**Критерии приемки:**
- [ ] Shift+клик или drag-select позволяет выбрать фразу
- [ ] Фраза сохраняется с типом PHRASE
- [ ] Фраза подсвечивается отдельно от отдельных слов
- [ ] Фраза попадает в review

### US-4: Автоматическое изучение при перелистывании
Как пользователь, я хочу чтобы оставшиеся синие слова автоматически становились известными при переходе на следующую страницу.

**Критерии приемки:**
- [ ] Настройка "Mark blue as known on page turn" (вкл/выкл)
- [ ] При перелистывании все NEW термины текущей страницы становятся KNOWN
- [ ] Bulk endpoint, не N+1 запросов
- [ ] Работает для PDF и текстового режима

### US-5: Review из Reader
Как пользователь, я хочу начать review по сохраненным словам прямо из reader, не уходя в deck library.

**Критерии приемки:**
- [ ] Счетчик "Review: N" показывает количество due карточек из текущего текста
- [ ] Кнопка "Review" запускает SRS-сессию
- [ ] После review возврат в reader на ту же позицию
- [ ] Фразы включены в review

## States и UI Flow

```
[Content Library] → [Open Text/PDF] → [Reader View]
                                      ↓
                    [Term Inspector] ← [Click Word/Phrase]
                                      ↓
                    [Create LingQ/Known/Ignore]
                                      ↓
                    [Page Turn] → [Bulk Mark Known?] → [Continue Reading]
                                      ↓
                    [Review Button] → [SRS Session] → [Back to Reader]
```

## Компоненты UI

### Reader View
- **Текстовая область:** paginated текст/PDF с подсветкой
- **Навигация:** страницы, главы, progress bar
- **Статус бар:** unique words, known %, review count
- **Sidebar:** оглавление, настройки

### Term Inspector (Side Panel)
- **Заголовок:** выбранное слово/фраза
- **Контекст:** предложение с выделенным термином
- **Перевод:** автоматический + поле своего значения
- **Действия:** Create LingQ (yellow), Known (white), Ignore (muted)
- **Дубликаты:** список существующих карточек с этим термином
- **Кнопка карточки:** "Create Card" (SRS)

### Phrase Selection
- **Визуальный фидбек:** подсветка выбранных слов
- **Макс длина:** 8 слов (настройка)
- **Отмена:** Escape или клик вне

## Отображение статусов

| Статус | Цвет | CSS класс (Tailwind) | Поведение |
|--------|------|----------------------|-----------|
| NEW | Синий | `text-blue-400` | Кликабельно, привлекает внимание |
| LINGQ | Желтый | `text-yellow-400` | Кликабельно, показывает meaning |
| KNOWN | Белый | `text-white` | Кликабельно, можно вернуть в LINGQ |
| IGNORED | Приглушенный | `text-white/40` | Не кликабельно, не в статистике |
| PHRASE | Оранжевый/Желтый | `text-orange-400 underline` | Приоритет над словами |

## API Integration

### Text Analyze
```typescript
POST /api/text/analyze
{
  text: string,
  projectId: string,
  language: string
}
→ {
  tokens: TextToken[],
  statuses: TermStatusMap,
  stats: TextStats
}
```

### Term Actions
```typescript
POST /api/terms
{ text, meaning, projectId, type: "WORD" | "PHRASE" }

POST /api/terms/mark-known
{ termId, projectId }

POST /api/terms/ignore
{ termId, projectId }

POST /api/terms/bulk-known
{ termIds: string[], projectId }
```

### Review from Reader
```typescript
GET /api/study/reader-review?projectId=X&context=reader&bookId=Y
→ { sessionId, dueCount, cards: CardPreview[] }

POST /api/study/session/{id}/review
{ cardId, rating: "again" | "hard" | "good" | "easy" }
```

## Настройки пользователя

```typescript
interface ReaderSettings {
  markBlueAsKnownOnPageTurn: boolean;
  autoTranslate: boolean;
  showPhraseHints: boolean;
  maxPhraseLength: number; // 2-8
  fontSize: "small" | "medium" | "large";
  lineHeight: "compact" | "normal" | "relaxed";
}
```

## Метрики успеха

- **Time to first LingQ:** < 10 секунд от открытия текста
- **Review completion rate:** > 70% начатых review-сессий
- **Page turn speed:** < 1 секунды для bulk-known операции
- **Term coverage:** > 90% слов текста имеют статус (не NEW)
