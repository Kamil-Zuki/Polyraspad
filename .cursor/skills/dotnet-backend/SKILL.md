# Skill: .NET Backend Work

Используй этот skill для работы с backend сервисами (VocabularyService, AggregatorService, MediaService, authorization-module).

## First Reads (обязательно)

1. Интерфейс сервиса и его реализация в `*/Services/`
2. DTO классы в `*/DTOs/` или `*.proto`
3. AutoMapper профили в `*/Mapping/`
4. Связанные тесты в `*.Tests/`
5. `.cursor/rules/04-csharp-aspnetcore-2026.mdc`
6. `.cursor/rules/06-lingq-domain-guardrails.mdc` (если Vocabulary)

## Архитектура сервисов

```
VocabularyService/
├── Services/           # Бизнес-логика
│   ├── TermService.cs
│   ├── CardService.cs
│   └── TextService.cs
├── DTOs/              # Data Transfer Objects
├── Grpc/              # gRPC сервисы
├── Data/              # EF Core DbContext
└── Migrations/        # EF миграции

AggregatorService/
├── Controllers/       # REST API
├── Services/          # Агрегация
└── Clients/           # Http/gRPC клиенты
```

## Технологический стек

- **.NET 9** — Runtime и SDK
- **ASP.NET Core** — Web framework
- **EF Core** — ORM
- **gRPC** — Интерсервисная коммуникация
- **AutoMapper** — Object mapping
- **FluentValidation** — Валидация
- **Polly** — Resilience
- **xUnit** — Тестирование

## Структура кода

### Сервисный слой

```csharp
// Интерфейс
public interface ITermService
{
    Task<TermDto> CreateAsync(CreateTermRequest request);
    Task<TermDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<TermDto>> GetStatusesAsync(
        IEnumerable<string> termTexts, 
        int projectId);
}

// Реализация
public class TermService : ITermService
{
    private readonly VocabularyDbContext _db;
    private readonly ILogger<TermService> _logger;
    private readonly IMapper _mapper;
    
    public TermService(
        VocabularyDbContext db,
        ILogger<TermService> logger,
        IMapper mapper)
    {
        _db = db;
        _logger = logger;
        _mapper = mapper;
    }
    
    public async Task<TermDto> CreateAsync(CreateTermRequest request)
    {
        var normalized = Normalize(request.Text);
        
        // Проверка дубликата по exact normalized text
        var existing = await _db.ProjectTerms
            .FirstOrDefaultAsync(t => 
                t.ProjectId == request.ProjectId && 
                t.NormalizedText == normalized);
        
        if (existing != null)
        {
            _logger.LogInformation("Term '{Text}' already exists as {TermId}", 
                request.Text, existing.Id);
            return _mapper.Map<TermDto>(existing);
        }
        
        var term = new ProjectTerm
        {
            Text = request.Text,
            NormalizedText = normalized,
            Type = DetectType(request.Text),
            ProjectId = request.ProjectId,
            Language = request.Language ?? "en"
        };
        
        _db.ProjectTerms.Add(term);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Created term {TermId} for '{Text}'", 
            term.Id, request.Text);
        
        return _mapper.Map<TermDto>(term);
    }
    
    private static string Normalize(string text) => 
        text.Trim().ToLowerInvariant();
    
    private static TermType DetectType(string text) =>
        text.Contains(' ') ? TermType.Phrase : TermType.Word;
}
```

### API слой (Minimal API)

```csharp
// Program.cs или отдельный extension method
app.MapPost("/api/terms", async (
    CreateTermRequest request,
    ITermService termService,
    IValidator<CreateTermRequest> validator) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
        return Results.BadRequest(validation.Errors);
    
    var term = await termService.CreateAsync(request);
    return Results.Created($"/api/terms/{term.Id}", term);
});

// Typed results для сложных сценариев
app.MapGet("/api/terms/{id}", 
    Results<Ok<TermDto>, NotFound, BadRequest> (int id, ITermService service) =>
        id <= 0
            ? TypedResults.BadRequest()
            : await service.GetByIdAsync(id) is { } term
                ? TypedResults.Ok(term)
                : TypedResults.NotFound());
```

### gRPC сервис

```protobuf
//Protos/term_service.proto
syntax = "proto3";

service TermService {
    rpc GetTerm (GetTermRequest) returns (TermDto);
    rpc CreateTerm (CreateTermRequest) returns (TermDto);
    rpc MarkTermKnown (MarkTermRequest) returns (StatusResponse);
    rpc BulkMarkKnown (BulkMarkRequest) returns (BulkMarkResponse);
}

message TermDto {
    int32 id = 1;
    string text = 2;
    string normalized_text = 3;
    TermType type = 4;
    TermStatus status = 5;
    string meaning = 6;
}

enum TermType {
    WORD = 0;
    PHRASE = 1;
}

enum TermStatus {
    NEW = 0;
    LINGQ = 1;
    KNOWN = 2;
    IGNORED = 3;
}
```

```csharp
// Grpc/TermGrpcService.cs
public class TermGrpcService : TermService.TermServiceBase
{
    private readonly ITermService _termService;
    private readonly IMapper _mapper;
    
    public TermGrpcService(ITermService termService, IMapper mapper)
    {
        _termService = termService;
        _mapper = mapper;
    }
    
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

### DTO и Mapping

```csharp
// DTOs/TermDto.cs
public record TermDto(
    int Id,
    string Text,
    string NormalizedText,
    TermType Type,
    TermStatus Status,
    string? Meaning);

// Mapping/TermProfile.cs
public class TermProfile : Profile
{
    public TermProfile()
    {
        CreateMap<ProjectTerm, TermDto>();
        CreateMap<CreateTermRequest, ProjectTerm>();
    }
}
```

### Валидация

```csharp
// Validators/CreateTermRequestValidator.cs
public class CreateTermRequestValidator : AbstractValidator<CreateTermRequest>
{
    public CreateTermRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(200)
            .Must(BeValidText).WithMessage("Text contains invalid characters");
        
        RuleFor(x => x.ProjectId)
            .GreaterThan(0);
        
        RuleFor(x => x.Language)
            .NotEmpty()
            .MaximumLength(10);
    }
    
    private static bool BeValidText(string text) =>
        !string.IsNullOrWhiteSpace(text);
}
```

## Миграции

### Безопасный процесс

```bash
# 1. Создать миграцию
dotnet ef migrations add AddNormalizedText --project VocabularyService

# 2. Применить (dev)
dotnet ef database update --project VocabularyService

# 3. Сгенерировать SQL для review
dotnet ef migrations script --project VocabularyService
```

### Код миграции

```csharp
// Migrations/20260101_AddNormalizedText.cs
public partial class AddNormalizedText : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Этап 1: Добавляем nullable
        migrationBuilder.AddColumn<string>(
            name: "NormalizedText",
            table: "ProjectTerms",
            type: "text",
            nullable: true);
        
        // Этап 2: Заполняем данные
        migrationBuilder.Sql(@"
            UPDATE ""ProjectTerms"" 
            SET ""NormalizedText"" = LOWER(TRIM(""Text""))
            WHERE ""NormalizedText"" IS NULL");
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NormalizedText",
            table: "ProjectTerms");
    }
}
```

## Тестирование

### Unit Tests

```csharp
// VocabularyService.Tests/Services/TermServiceTests.cs
public class TermServiceTests
{
    [Fact]
    public void NormalizeText_TrimsAndLowercases()
    {
        var result = TermService.NormalizeText("  Hello World  ");
        Assert.Equal("hello world", result);
    }
    
    [Theory]
    [InlineData("sleep", "slept")]
    [InlineData("go", "went")]
    public void DifferentForms_AreNotDuplicates(string form1, string form2)
    {
        var norm1 = TermService.NormalizeText(form1);
        var norm2 = TermService.NormalizeText(form2);
        Assert.NotEqual(norm1, norm2);
    }
}
```

### Integration Tests

```csharp
// VocabularyService.Tests/Integration/TermsApiTests.cs
public class TermsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public TermsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateTerm_Returns201_WithValidRequest()
    {
        var request = new { text = "hello", projectId = 1, language = "en" };
        
        var response = await _client.PostAsJsonAsync("/api/terms", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var term = await response.Content.ReadFromJsonAsync<TermDto>();
        term.Text.Should().Be("hello");
    }
}
```

## Команды для работы

```bash
# Сборка
dotnet build

# Тесты
dotnet test
dotnet test --filter "FullyQualifiedName~TermService"

# Миграции
dotnet ef migrations add <Name> --project <Project>
dotnet ef database update --project <Project>

# Форматирование
dotnet format
```

## LingQ-specific проверки

- [ ] Используется `ProjectTerm`, не `ProjectLemma`
- [ ] Нормализация: trim + lowercase
- [ ] Дубликаты проверяются по `NormalizedText`
- [ ] Фразы — `Type = TermType.Phrase`
- [ ] Связь карточки через `ProjectTermId`
