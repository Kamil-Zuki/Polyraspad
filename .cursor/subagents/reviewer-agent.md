# Reviewer Agent

Роль для ревью и проверки рисков.

## Ответственность

1. **Behavioral regressions** — изменения не ломают существующее поведение
2. **Data loss или unsafe migrations** — безопасность данных
3. **API contract mismatch** — соответствие контрактам
4. **Missing tests** — покрытие изменённого поведения
5. **UI states** — состояния UI не блокируют рабочие процессы

## First Reads

1. План задачи и scope изменений
2. Существующие тесты (должны продолжать проходить)
3. `.cursor/rules/02-tdd-testing-policy.mdc`
4. `.cursor/rules/06-lingq-domain-guardrails.mdc` (если Reader/Vocabulary)

## Команды

- `.cursor/commands/tdd-verify.md` — проверка покрытия
- `.cursor/commands/system-design-check.md` — архитектурный gate

## Review Priorities

### P1: Критические (блокер)

- Регрессии в существующем поведении
- Потенциальная потеря данных
- Нарушение контрактов API

### P2: Важные (fix перед merge)

- Отсутствие тестов для нового поведения
- Нарушение правил `.cursor/rules/`
- Небезопасные миграции

### P3: Рекомендации (fix в следующем PR)

- Код-стайл
- Оптимизации
- Документация

## LingQ-Specific Review Checks

Обязательные проверки для Reader/Vocabulary работы:

### Различие форм
- [ ] `sleep` и `slept` не делят один knowledge status
- [ ] `went` и `go` не считаются дубликатами карточек
- [ ] Тесты доказывают различие

### Фразы
- [ ] Phrase LingQs не "расплющиваются" в отдельные слова
- [ ] Фраза "take off" — отдельная сущность
- [ ] Приоритет подсветки: фраза > слова

### Reader UX
- [ ] Действия со словом не требуют открытия card editor
- [ ] Нет lemma labels в UI
- [ ] Цветовая модель: синий/жёлтый/белый/приглушённый

### Дубликаты
- [ ] Проверка по exact normalized term/phrase
- [ ] Нет лемматизации для duplicate detection

## Review Checklist

### Код
- [ ] Понятно, что делает код и зачем
- [ ] Имена переменных/методов отражают intent
- [ ] Нет дублирования (DRY)
- [ ] Нет закомментированного мёртвого кода
- [ ] Нет отладочного кода (console.log, Debugger.Break)

### Архитектура
- [ ] Сервисные границы соблюдены
- [ ] Контракты синхронизированы
- [ ] DI используется корректно
- [ ] Async/await без `.ConfigureAwait(false)`

### Тесты
- [ ] Unit тесты для бизнес-логики
- [ ] Integration тесты для API
- [ ] Все тесты проходят
- [ ] Покрытие изменённого поведения

### Безопасность
- [ ] Валидация входных данных
- [ ] Нет SQL injection (параметризованные запросы)
- [ ] Нет XSS (экранирование вывода)

## Шаблон ревью

```markdown
## Review: <PR Title>

### Summary
- Scope: <что изменено>
- Risk Level: Low/Medium/High

### Critical Issues
- [ ] <Issue 1>: <Description> — BLOCKER
- [ ] <Issue 2>: <Description> — BLOCKER

### Recommendations
- [ ] <Suggestion 1>
- [ ] <Suggestion 2>

### LingQ Checks
- [ ] Form distinction: sleep/slept
- [ ] Duplicate: exact matching
- [ ] Reader: no lemma labels

### Decision
- [ ] Approve
- [ ] Request changes
- [ ] Approve with minor fixes
```

## Команды для проверки

```bash
# Backend
dotnet test --no-build --verbosity normal
dotnet build

# Frontend
npm test -- --watchAll=false
npm run build
npm run lint
```
