# Frontend Agent

Роль для работы с `polyraspad-frontend/` — Next.js UI, Reader UX, состояние и компоненты.

## Ответственность

- Следовать существующим паттернам Next.js и React Query
- Поддерживать Reader как эффективный инструмент чтения (не маркетинговую страницу)
- Предпочитать чистые инструментальные поверхности декоративному UI
- Обеспечивать работу на desktop и mobile

## First Reads (обязательно)

1. Релевантная страница/компонент
2. Существующие hooks в `src/lib/react-query/`
3. API клиенты в `src/lib/api/`
4. `.cursor/rules/03-nextjs-2026.mdc`
5. `.cursor/rules/06-lingq-domain-guardrails.mdc` (если работа с Reader/Vocabulary)

## Команды

При работе используй:
- `.cursor/commands/tdd-start.md` — начало фичи
- `.cursor/commands/nextjs-slice.md` — чеклист Next.js
- `.cursor/commands/tdd-verify.md` — проверка перед завершением

## Reader UX Rules

### Цветовая модель
- **Синий (NEW)** — новое слово/фраза
- **Жёлтый (LINGQ)** — сохранённое слово/фраза со значением
- **Белый (KNOWN)** — известное слово
- **Приглушённый (IGNORED)** — игнорируемое слово

### Критические запреты
- ❌ Не показывать lemma labels в UI Reader
- ❌ Не требовать навигации из Reader для действий со словом
- ❌ Не использовать леммы как основу для статусов

### Обязательные паттерны
- ✅ Все действия (LingQ, Known, Ignore, Review) доступны из Reader
- ✅ Инспектор слова открывается по клику без перезагрузки
- ✅ Карточка — отдельное SRS-действие, не обязательное

## Реализация

### Server Components (по умолчанию)

```tsx
// page.tsx — async Server Component
export default async function ReaderPage({ 
  params 
}: { 
  params: { id: string } 
}) {
  const text = await fetchText(params.id);
  const analysis = await analyzeText(text.content);
  
  return <Reader text={text} tokens={analysis.tokens} />;
}
```

### Client Components (только для интерактивности)

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
      <h3 className="text-lg font-semibold text-white">{term.text}</h3>
      <input 
        value={meaning}
        onChange={(e) => setMeaning(e.target.value)}
        className="bg-app-bg border border-white/10 text-white"
      />
      <button onClick={() => saveMutation.mutate({ id: term.id, meaning })}>
        Save
      </button>
    </div>
  );
}
```

### Styling

```tsx
// Цвета из дизайн-системы
<div className="bg-app-bg">           {/* #0B0F15 */}
<div className="bg-app-surface">      {/* #131927 */}
<div className="text-brand-primary">  {/* #8B5CF6 */}

// Glass panel
<div className="bg-[#131927]/70 backdrop-blur-md border border-white/10">
```

## Тестирование

```typescript
// Component test
it('renders yellow highlight for saved LingQ terms', () => {
  render(<Reader terms={[{ text: 'hello', status: 'LINGQ' }]} />);
  expect(screen.getByText('hello')).toHaveClass('text-yellow-400');
});

// User interaction
it('marks term as known when clicking Known button', async () => {
  render(<TermInspector term={mockTerm} />);
  await userEvent.click(screen.getByText('Known'));
  expect(mockMarkKnown).toHaveBeenCalledWith(mockTerm.id);
});
```

## LingQ-specific проверки

При изменениях в Reader:
- [ ] `sleep` и `slept` — разные визуальные статусы
- [ ] Фраза "take off" подсвечивается как целое, не как отдельные слова
- [ ] Phrase selection работает через Shift+click
- [ ] Счётчики страницы: new terms, LingQs, known %, review

## Поиск по коду

```bash
# Найти похожие компоненты
rg "TermCard|WordHighlight" src/components/

# Найти hooks
rg "useQuery|useMutation" src/lib/react-query/

# Найти API клиенты
rg "export.*function.*term" src/lib/api/
```
