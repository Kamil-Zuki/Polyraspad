# System Design Check — Архитектурный gate

Используй перед merge или при значительных архитектурных изменениях.

## Чеклист

### 1. Service Boundaries
- [ ] Изменения не нарушают границы сервисов
- [ ] Нет прямых обращений к БД другого сервиса
- [ ] Все межсервисные вызовы через публичный API (REST/gRPC)

### 2. Contract Compatibility
- [ ] DTO/gRPC протоколы синхронизированы
- [ ] Нет breaking changes для существующих клиентов
- [ ] Если breaking — есть versioning или migration plan

### 3. Resilience
- [ ] Внешние вызовы имеют retry policy
- [ ] Circuit breaker для ненадёжных зависимостей
- [ ] Graceful degradation при недоступности сервиса

```csharp
// Retry с Polly
builder.Services.AddHttpClient("external")
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Circuit breaker
builder.Services.AddHttpClient("critical")
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

### 4. Observability
- [ ] Структурированное логирование ключевых операций
- [ ] Distributed tracing для cross-service calls
- [ ] Метрики для бизнес-операций (terms created, cards reviewed)

```csharp
// Логирование
_logger.LogInformation(
    "Processing term {TermId} for user {UserId}",
    termId, userId);

// Tracing
using var activity = new Activity("CreateTerm").Start();
activity?.SetTag("term.project_id", projectId);
```

### 5. Idempotency
- [ ] Операции, которые можно вызвать повторно — идемпотентны
- [ ] Используется idempotency key для critical operations

```csharp
// Идемпотентная операция
public async Task<TermDto> CreateOrUpdateTermAsync(
    CreateTermRequest request,
    string idempotencyKey)
{
    var existing = await _idempotencyStore.GetAsync(idempotencyKey);
    if (existing != null) return existing;
    
    var term = await _termService.CreateAsync(request);
    await _idempotencyStore.SaveAsync(idempotencyKey, term);
    return term;
}
```

### 6. Migration Safety
- [ ] Миграции неразрушающие (nullable новые поля)
- [ ] Есть план заполнения данных
- [ ] Есть rollback plan

### 7. Backward Compatibility
- [ ] Новые поля optional/nullable
- [ ] Старые endpoint'ы работают
- [ ] Deprecation notices для obsolete полей

```csharp
// Новое поле — optional
public string? NewField { get; set; }

// Старое поле — obsolete
[Obsolete("Use NewField instead")]
public string OldField { get; set; }
```

### 8. Performance Considerations
- [ ] N+1 запросы устранены (Include, projection)
- [ ] Тяжёлые операции async
- [ ] Кэширование там, где это имеет смысл

### 9. Security
- [ ] Валидация всех входных данных
- [ ] Authorization на endpoints
- [ ] Нет sensitive data в логах

### 10. LingQ Domain (если применимо)
- [ ] Term-first модель соблюдена
- [ ] Нет новых зависимостей от лемм
- [ ] Exact matching для дубликатов

## Решение

| Проверка | Статус | Действие |
|----------|--------|----------|
| Все критичные | ✅ | Можно merge |
| Есть warning'и | ⚠️ | Задокументировать, fix в следующем PR |
| Есть блокеры | ❌ | Fix перед merge |

## Шаблон записи о рисках

```markdown
## System Design Check: <Feature Name>
Date: 2026-XX-XX

### Passed
- Service boundaries maintained
- Contracts synchronized
- Resilience patterns applied

### Risks Identified
- <Risk 1>: <Mitigation>
- <Risk 2>: <Mitigation>

### Action Items
- [ ] <Follow-up task 1>
- [ ] <Follow-up task 2>
```
