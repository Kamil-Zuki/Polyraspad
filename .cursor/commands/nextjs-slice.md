# Next.js Slice — Чеклист для App Router задач

Используй при работе с фронтендом на Next.js.

## Подготовка
- [ ] Прочитать `.cursor/skills/nextjs-frontend/SKILL.md`
- [ ] Прочитать `.cursor/rules/03-nextjs-2026.mdc`
- [ ] Найти похожие компоненты через `rg` в `src/components/`, `src/app/`

## Чеклист реализации

### 1. Data Fetching

#### Server Component (по умолчанию)
- [ ] Используется `async` компонент
- [ ] Fetch с явным `cache` режимом:
```tsx
// Динамические данные
cache: 'no-store'

// Статические данные
cache: 'force-cache'

// ISR
next: { revalidate: 60 }
```
- [ ] Обработка ошибок через `error.tsx`

#### Client Component (только когда нужно)
- [ ] `'use client'` директива в начале файла
- [ ] Используется React Query для server state
- [ ] Локальный state — `useState`, `useReducer`

### 2. Type Safety
- [ ] DTO интерфейсы соответствуют backend контрактам
- [ ] Нет `any` — строгая типизация
- [ ] Props типизированы через `interface`

### 3. Styling
- [ ] Tailwind CSS классы из дизайн-системы
- [ ] Цвета: `bg-app-bg`, `bg-app-surface`, `text-brand-primary`
- [ ] Lucide React иконки

### 4. Reader-specific (если работа с Reader)
- [ ] Нет lemma labels в UI
- [ ] Цветовая модель: синий(NEW), жёлтый(LINGQ), белый(KNOWN)
- [ ] Инспектор термина без навигации
- [ ] Phrase selection работает (Shift+click)

### 5. Тестирование
- [ ] Component test для нового поведения
- [ ] Проверка loading/error состояний
- [ ] Проверка user interactions

### 6. Проверки перед коммитом
```bash
# Сборка
npm run build

# Тесты
npm test -- --watchAll=false

# Линтер
npm run lint
```

## Паттерны

### Server Component с данными
```tsx
// app/reader/page.tsx
export default async function ReaderPage({ 
  params 
}: { 
  params: { id: string } 
}) {
  const text = await fetchText(params.id);
  const analysis = await analyzeText(text.content);
  
  return (
    <Reader 
      text={text} 
      tokens={analysis.tokens}
      termStatuses={analysis.statuses}
    />
  );
}
```

### Client Component с интерактивностью
```tsx
'use client'

import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';

export function TermInspector({ term }: { term: Term }) {
  const [meaning, setMeaning] = useState(term.meaning || '');
  const queryClient = useQueryClient();
  
  const saveMutation = useMutation({
    mutationFn: saveTermMeaning,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['terms'] });
    }
  });
  
  return (
    <div className="glass-panel p-4">
      <h3 className="text-lg font-semibold">{term.text}</h3>
      <input 
        value={meaning}
        onChange={(e) => setMeaning(e.target.value)}
        className="bg-app-bg border border-white/10"
      />
      <button 
        onClick={() => saveMutation.mutate({ id: term.id, meaning })}
        className="btn-primary"
      >
        Save
      </button>
    </div>
  );
}
```

## Common Pitfalls

- ❌ Не используйте `fetch` без явного `cache` режима
- ❌ Не делайте все компоненты Client Components
- ❌ Не забывайте `key` prop при рендеринге списков
- ❌ Не используйте `any` — типизируйте всё
