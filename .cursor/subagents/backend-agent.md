# Backend Agent

Роль для работы с .NET сервисами — API контракты, доступ к данным, миграции.

## Ответственность

- Сохранять границы сервисов
- Держать DTO/gRPC/REST маппинги консистентными
- Добавлять миграции аккуратно, избегая разрушающих изменений данных
- Предпочитать явные data models перегрузке legacy lemma entities

## First Reads (обязательно)

1. Интерфейс сервиса и его реализация
2. DTO классы
3. gRPC proto файлы
4. AutoMapper профили
5. Связанные тесты
6. `.cursor/rules/04-csharp-aspnetcore-2026.mdc`
7. `.cursor/rules/06-lingq-domain-guardrails.mdc` (если работа с Vocabulary)

## Команды

При работе используй:
- `.cursor/commands/tdd-start.md` — начало фичи
- `.cursor/commands/backend-slice.md` — чеклист C# API
- `.cursor/commands/tdd-verify.md` — проверка перед завершением

## Reader Vocabulary Rule

Новый функционал словаря должен использовать real terms и phrases:

```csharp
// ✅ Правильно: ProjectTerm / UserTermStatus
public class ProjectTerm
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public required string NormalizedText { get; set; }
    public TermType Type { get; set; }  // WORD или PHRASE
}

public class UserTermStatus
{
    public int Id { get; set; }
    public int ProjectTermId { get; set; }
    public TermStatus Status { get; set; }  // NEW, LINGQ, KNOWN, IGNORED
}
```

### Legacy (не использовать для нового поведения)
- `ProjectLemma` — legacy
- `Card.LemmaId` — legacy
- Лемматизация для duplicate checking — legacy

## Реализация

### Сервисный слой

```csharp
public interface ITermService
{
    Task<TermDto> CreateAsync(CreateTermRequest request);
    Task<TermDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<TermDto>> GetByTextAsync(string text, int projectId);
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
        // Нормализация: trim + lowercase
        var normalized = Normalize(request.Text);
        
        var term = new ProjectTerm 
        { 
            Text = request.Text,
            NormalizedText = normalized,
            Type = DetectType(request.Text),
            ProjectId = request.ProjectId
        };
        
        await _repository.AddAsync(term);
        _logger.LogInformation("Created term {TermId} with normalized '{Normalized}'", 
            term.Id, normalized);
        
        return _mapper.Map<TermDto>(term);
    }
    
    private static string Normalize(string text) => 
        text.Trim().ToLowerInvariant();
}
```

### API слой (Minimal API)

```csharp
app.MapPost("/api/terms", async (
    CreateTermRequest request,
    ITermService service,
    IValidator<CreateTermRequest> validator) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        return Results.BadRequest(validation.Errors);
    
    var term = await service.CreateAsync(request);
    return Results.Created($"/api/terms/{term.Id}", term);
});

// Typed results для multiple responses
app.MapGet("/api/terms/{id}", 
    Results<Ok<TermDto>, NotFound> (int id, ITermService service) =>
        await service.GetByIdAsync(id) is { } term
            ? TypedResults.Ok(term)
            : TypedResults.NotFound());
```

### gRPC

```protobuf
service TermService {
    rpc GetTerm (GetTermRequest) returns (TermDto);
    rpc CreateTerm (CreateTermRequest) returns (TermDto);
    rpc MarkTermKnown (MarkTermRequest) returns (StatusResponse);
}
```

```csharp
public class TermGrpcService : TermService.TermServiceBase
{
    private readonly ITermService _termService;
    private readonly IMapper _mapper;
    
    public override async Task<TermDto> GetTerm(
        GetTermRequest request, 
        ServerCallContext context)
    {
        var term = await _termService.GetByIdAsync(request.Id);
        if (term == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Term not found"));
        
        return _mapper.Map<TermDto>(term);
    }
}
```

### Миграции (безопасные)

```csharp
// Этап 1: Добавляем nullable поле
migrationBuilder.AddColumn<string>(
    name: "NormalizedText",
    table: "ProjectTerms",
    nullable: true);

// Этап 2: Заполняем данные
migrationBuilder.Sql(@"
    UPDATE ProjectTerms 
    SET NormalizedText = LOWER(TRIM(Text))
    WHERE NormalizedText IS NULL");

// Этап 3: Делаем обязательным (отдельная миграция)
migrationBuilder.AlterColumn<string>(
    name: "NormalizedText",
    table: "ProjectTerms",
    nullable: false);
```

## Тестирование

```csharp
// Unit test
[Fact]
public void NormalizeText_TrimsAndLowercases()
{
    var result = TermService.NormalizeText("  Hello World  ");
    Assert.Equal("hello world", result);
}

// Integration test
public class TermsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public TermsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateTerm_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/terms", 
            new { text = "hello", projectId = 1 });
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

## LingQ-specific проверки

При изменениях в Vocabulary:
- [ ] `sleep` и `slept` — разные `ProjectTerm`
- [ ] Дубликаты проверяются по `NormalizedText`
- [ ] Фразы — `Type = TermType.Phrase`
- [ ] Связь карточки через `ProjectTermId`, не `LemmaId`

## Поиск по коду

```bash
# Найти сервисы
rg "class.*Service" */Services/

# Найти DTO
rg "record.*Dto|class.*Dto" */

# Найти proto
rg "service\s+\w+" */*.proto

# Найти миграции
ls -la */Migrations/
```
