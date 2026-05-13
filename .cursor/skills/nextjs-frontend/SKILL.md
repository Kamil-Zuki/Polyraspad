# Skill: Next.js Frontend Work

Используй этот skill для работы с `polyraspad-frontend/`.

## First Reads (обязательно)

1. Релевантная страница/компонент в `src/app/`, `src/components/`
2. Существующие hooks в `src/lib/react-query/`
3. API клиенты в `src/lib/api/`
4. `.cursor/rules/03-nextjs-2026.mdc`
5. `polyraspad-frontend/.cursor/rules/` (дизайн-система)

## Архитектура проекта

```
polyraspad-frontend/
├── src/
│   ├── app/              # Next.js App Router
│   │   ├── reader/       # Reader page
│   │   ├── library/      # Library page
│   │   └── api/          # Route handlers
│   ├── components/       # React компоненты
│   │   ├── ui/           # Базовые UI компоненты
│   │   └── reader/       # Reader-specific
│   ├── lib/
│   │   ├── api/          # API клиенты
│   │   ├── react-query/  # Hooks для данных
│   │   └── types/        # TypeScript типы
│   └── styles/           # Tailwind + globals
```

## Технологический стек

- **Next.js 15** — App Router, Server Components по умолчанию
- **React 19** — Modern hooks
- **TypeScript** — Строгая типизация
- **Tailwind CSS** — Utility-first стили
- **TanStack Query** — Server state management
- **Lucide React** — Иконки

## Data Fetching

### Server Component (по умолчанию)

```tsx
// app/reader/page.tsx
import { fetchText, analyzeText } from '@/lib/api';

export default async function ReaderPage({ 
  params 
}: { 
  params: { id: string } 
}) {
  // Fetch на сервере
  const text = await fetchText(params.id, { cache: 'no-store' });
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

### Client Component (только при необходимости)

```tsx
'use client'

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fetchTerms, saveTerm } from '@/lib/api';

export function TermList({ projectId }: { projectId: string }) {
  const { data, isLoading, error } = useQuery({
    queryKey: ['terms', projectId],
    queryFn: () => fetchTerms(projectId),
  });
  
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: saveTerm,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['terms'] });
    },
  });
  
  if (isLoading) return <Skeleton />;
  if (error) return <ErrorDisplay error={error} />;
  
  return (
    <ul>
      {data?.map(term => (
        <li key={term.id}>{term.text}</li>
      ))}
    </ul>
  );
}
```

## Styling

### Дизайн-система

```tsx
// Цвета
<div className="bg-app-bg">           {/* #0B0F15 */}
<div className="bg-app-surface">      {/* #131927 */}
<div className="hover:bg-app-hover">  {/* #1C2438 */}
<div className="text-brand-primary">  {/* #8B5CF6 */}

// Glass panel (часто используется)
<div className="glass-panel">
  {/* Содержимое */}
</div>

// Кнопки
<button className="btn-primary">      {/* Градиент + glow */}
<button className="btn-secondary">   {/* Ghost style */}

// Инпуты
<input className="input-app" />
```

### Tailwind конфиг

```typescript
// tailwind.config.ts
export default {
  theme: {
    extend: {
      colors: {
        'app-bg': '#0B0F15',
        'app-surface': '#131927',
        'app-hover': '#1C2438',
        'brand-primary': '#8B5CF6',
        'brand-secondary': '#3B82F6',
      },
    },
  },
}
```

## Reader-specific паттерны

### Term Highlighting

```tsx
function getTermClass(status: TermStatus): string {
  switch (status) {
    case 'NEW': return 'text-blue-400 cursor-pointer hover:underline';
    case 'LINGQ': return 'text-yellow-400 cursor-pointer hover:underline';
    case 'KNOWN': return 'text-white';
    case 'IGNORED': return 'text-white/40';
    default: return 'text-white';
  }
}

// Phrase highlighting имеет приоритет
function renderTokens(tokens: Token[]) {
  // Сначала ищем фразы
  const phrases = findPhrases(tokens);
  
  return tokens.map((token, i) => {
    const phrase = phrases.find(p => p.startIndex === i);
    if (phrase) {
      return (
        <span key={i} className="text-yellow-400 underline">
          {tokens.slice(i, i + phrase.length).map(t => t.text).join(' ')}
        </span>
      );
    }
    
    return (
      <span key={i} className={getTermClass(token.status)}>
        {token.text}
      </span>
    );
  });
}
```

### Term Inspector

```tsx
'use client'

export function TermInspector({ term, onClose }: TermInspectorProps) {
  const [meaning, setMeaning] = useState(term.meaning || '');
  
  const createLingq = useMutation({
    mutationFn: () => createOrUpdateTerm({
      text: term.text,
      meaning,
      status: 'LINGQ',
    }),
  });
  
  return (
    <div className="glass-panel p-4 w-80">
      <h3 className="text-lg font-semibold text-white">{term.text}</h3>
      <p className="text-sm text-white/60 mb-4">{term.sentence}</p>
      
      <input
        value={meaning}
        onChange={(e) => setMeaning(e.target.value)}
        placeholder="Meaning..."
        className="input-app w-full mb-2"
      />
      
      <div className="flex gap-2">
        <button 
          onClick={() => createLingq.mutate()}
          className="btn-primary flex-1"
        >
          Create LingQ
        </button>
        <button className="btn-secondary">Known</button>
        <button className="btn-secondary">Ignore</button>
      </div>
    </div>
  );
}
```

## Тестирование

### Jest + React Testing Library

```typescript
// __tests__/Reader.test.tsx
import { render, screen, fireEvent } from '@testing-library/react';
import { Reader } from '@/components/reader/Reader';

describe('Reader', () => {
  it('renders blue highlight for new terms', () => {
    render(<Reader terms={[{ text: 'hello', status: 'NEW' }]} />);
    expect(screen.getByText('hello')).toHaveClass('text-blue-400');
  });
  
  it('opens term inspector on word click', async () => {
    render(<Reader terms={[{ text: 'hello', status: 'NEW' }]} />);
    fireEvent.click(screen.getByText('hello'));
    expect(screen.getByText('Create LingQ')).toBeInTheDocument();
  });
});
```

## Команды для работы

```bash
# Запуск dev сервера
cd polyraspad-frontend && npm run dev

# Сборка
npm run build

# Тесты
npm test
npm test -- --watchAll=false

# Линтер
npm run lint
```

## LingQ-specific проверки

- [ ] Термины показываются своими цветами статуса
- [ ] Фразы имеют приоритет отображения над словами
- [ ] Инспектор открывается по клику
- [ ] Нет lemma labels в UI
- [ ] Все действия доступны без покидания Reader
