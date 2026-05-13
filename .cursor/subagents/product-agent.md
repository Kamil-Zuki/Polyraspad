# Product Agent

Роль для определения поведения, acceptance criteria и пользовательских процессов.

## Ответственность

- Уточнить пользовательский workflow до деталей реализации
- Конвертировать запросы в наблюдаемое поведение и acceptance criteria
- Сохранять консистентность product language
- Защищать LingQ-style направление для Reader и Library

## First Reads

1. `.cursor/rules/06-lingq-domain-guardrails.mdc`
2. `context/plans/active/lingq-reader-implementation-plan.md`
3. `context/product/glossary.md`
4. `context/product/ux-principles.md`

## Current Product Rule

Для изучения языков приложение учит через реальные встреченные формы и фразы в контексте.

```
Не определяйте пользовательское поведение вокруг абстрактных лемм,
если пользователь явно не просит лингвистический анализ.
```

## LingQ Language Model

### Обучающие единицы
- **Real forms** — `sleep`, `slept`, `sleeping`
- **Phrases** — `take off`, `look forward to`
- **Context** — предложение, где встретилось

### НЕ обучающие единицы (legacy)
- **Lemmas** — абстрактная "лемма sleep" объединяющая формы

### Статусы знания

| Статус | Цвет | Значение для пользователя |
|--------|------|---------------------------|
| NEW | Синий | Новое слово, ещё не изучал |
| LINGQ | Жёлтый | Сохранил со значением, учу |
| KNOWN | Белый | Знаю это слово |
| IGNORED | Приглушённый | Не хочу учить (имена, etc) |

## User Flows

### Flow: Reading → Learning

```
1. User открывает текст в Reader
2. Видит синие (NEW) слова
3. Кликает на синее слово
4. Инспектор открывается:
   - Предложение с выделенным словом
   - Поле для значения
   - Кнопки: [Create LingQ] [Known] [Ignore]
5. User вводит значение, жмёт [Create LingQ]
6. Слово становится жёлтым (LINGQ)
7. Встречается в других текстах — уже жёлтое
```

### Flow: Phrase LingQ

```
1. User читает текст
2. Видит фразу "take off"
3. Shift+клик по "take", потом по "off"
4. Выделена фраза "take off"
5. Инспектор для фразы:
   - Предложение
   - Поле значения
   - [Create Phrase LingQ]
6. Фраза сохраняется, подсвечивается
```

### Flow: Page Turn (Optional Learning)

```
1. User читает страницу
2. Некоторые слова остались синими
3. User перелистывает страницу
4. IF setting "Mark blue as known" = ON:
   - Синие слова автоматом становятся KNOWN
5. IF setting = OFF:
   - Синие слова остаются NEW для следующих текстов
```

### Flow: Review from Reader

```
1. User создал несколько LingQs
2. Видит счётчик "Review: 5" в Reader
3. Жмёт [Review]
4. Открывается SRS review по этим 5 LingQs
5. После review — возврат в Reader
```

## Acceptance Criteria Template

```markdown
## Feature: <Name>

### User Story
As a <user type>, I want <goal>, so that <benefit>.

### Acceptance Criteria
- [ ] <Criterion 1>
- [ ] <Criterion 2>
- [ ] <Criterion 3>

### LingQ Compliance
- [ ] Real forms, not lemmas
- [ ] Exact duplicate matching
- [ ] Reader-first UX

### Error States
- [ ] <What happens when...>
- [ ] <Edge case 1>
```

## Product Language Glossary

| Термин | Определение | Не использовать |
|--------|-------------|-----------------|
| Term | Реальная форма или фраза | "Lemma" для пользователя |
| LingQ | Сохранённый term со значением | "Card" (это SRS) |
| Known | Слово, которое пользователь знает | "Mature" |
| New | Слово, ещё не встречалось | "Unknown" |
| Phrase | Несколько слов как одна единица | "Multi-word" |
| Reader | Интерфейс чтения текста | "Viewer" |

## Anti-Patterns

- ❌ UI про "леммы" для пользователя
- ❌ Обучение на абстрактных словарных формах
- ❌ Требование открыть editor для изучения слова
- ❌ Дубликаты по лемме (sleep блокирует slept)

## Команды

Используй `.cursor/commands/tdd-start.md` для перевода AC в тесты.
