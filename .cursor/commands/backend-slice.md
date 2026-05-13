# Backend Slice — Чеклист для C# API задач

Используй при работе с .NET бэкендом.

## Подготовка
- [ ] Прочитать `.cursor/skills/dotnet-backend/SKILL.md`
- [ ] Прочитать `.cursor/rules/04-csharp-aspnetcore-2026.mdc`
- [ ] Найти похожие сервисы через `rg` в `*/Services/`

## Чеклист реализации

### 1. Контракты
- [ ] DTO классы/records в `*/DTOs/` или `*.proto`
- [ ] REST и gRPC контракты синхронизированы
- [ ] Валидация через FluentValidation или Data Annotations

### 2. Сервисный слой
- [ ] Интерфейс сервиса явный
- [ ] Реализация в отдельном классе
- [ ] DI через конструктор
- [ ] Async/await без `.ConfigureAwait(false)`

```csharp
public interface ITermService
{
    Task<TermDto> CreateAsync(CreateTermRequest request);
    Task<TermDto?> GetByIdAsync(int id);
}

public class TermService : ITermService
{
    private readonly IRepository<ProjectTerm> _repository;
    private readonly ILogger<TermService> _logger;
    
    public TermService(
        IRepository<ProjectTerm> repository,
        ILogger<TermService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<TermDto> CreateAsync(CreateTermRequest request)
    {
        var term = new ProjectTerm 
        { 
            Text = request.Text,
            NormalizedText = Normalize(request.Text)
        };
        
        await _repository.AddAsync(term);
        _logger.LogInformation("Created term {TermId}", term.Id);
        
        return _mapper.Map<TermDto>(term);
    }
}
```

### 3. API слой
- [ ] Minimal API или Controller — консистентно с существующим кодом
- [ ] Typed results для multiple response types
- [ ] Обработка ошибок (ProblemDetails)

```csharp
// Minimal API
app.MapPost("/api/terms", async (
    CreateTermRequest request,
    ITermService service) =>
{
    var result = await service.CreateAsync(request);
    return Results.Created($"/api/terms/{result.Id}", result);
});

// Или с typed results
app.MapPost("/api/terms", async (
    CreateTermRequest request,
    ITermService service,
    IValidator<CreateTermRequest> validator) =>
    validator.Validate(request) is { IsValid: false } validation
        ? Results.BadRequest(validation.Errors)
        : await service.CreateAsync(request) is { } term
            ? Results.Created($"/api/terms/{term.Id}", term)
            : Results.Problem("Failed to create term"));
```

### 4. Данные
- [ ] Миграции: неразрушающие, с заполнением данных
- [ ] EF Core: явные `Include`, `AsNoTracking` где нужно
- [ ] Nullable поля для новых колонок (этап 1)

### 5. LingQ-specific (если Term/Vocabulary)
- [ ] Используется `ProjectTerm`, не `ProjectLemma`
- [ ] Нормализация: trim + lowercase
- [ ] Проверка дубликатов по `NormalizedText`
- [ ] Фразы — `Type = TermType.Phrase`

### 6. Тестирование
- [ ] Unit тесты для сервисов
- [ ] Integration тесты с `WebApplicationFactory`
- [ ] Контрактные тесты для API

```bash
# Запуск тестов
dotnet test --filter "FullyQualifiedName~TermService"
dotnet test --filter "FullyQualifiedName~TermsApi"
```

### 7. Проверки перед коммитом
```bash
# Сборка
dotnet build

# Тесты
dotnet test

# Форматирование
dotnet format --verify-no-changes
```

## Паттерны

### Typed Results
```csharp
app.MapGet("/api/terms/{id}", 
    Results<Ok<TermDto>, NotFound, BadRequest> (int id) =>
        id <= 0
            ? TypedResults.BadRequest()
            : await service.GetByIdAsync(id) is { } term
                ? TypedResults.Ok(term)
                : TypedResults.NotFound());
```

### Options Pattern
```csharp
// Конфигурация
public class TranslationOptions
{
    public required string ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

// Program.cs
builder.Services.Configure<TranslationOptions>(
    builder.Configuration.GetSection("Translation"));

// Использование
public class TranslationService
{
    public TranslationService(IOptions<TranslationOptions> options) { }
}
```

## Common Pitfalls

- ❌ Не используйте `ConfigureAwait(false)` в ASP.NET Core
- ❌ Не делайте сервисы зависимыми от `HttpContext`
- ❌ Не используйте `dynamic` для DTO
- ❌ Не забывайте валидировать входные данные
- ❌ Не делайте destructive миграции без explicit cleanup plan
