# Skill: Testing

Используй этот skill при добавлении или проверке поведения.

## Стратегия

Начинай с минимального теста, который доказывает изменённое поведение:

1. **Unit test** — для изолированной бизнес-логики
2. **Integration test** — для API контрактов, миграций, shared flows
3. **E2E test** — для критических пользовательских путей

## Уровни тестов

### Backend (.NET + xUnit)

| Тип | Когда | Пример |
|-----|-------|--------|
| Unit | Изменение логики сервиса | `TermService.NormalizeText` |
| Integration | API endpoint | `POST /api/terms` возвращает 201 |
| Contract | DTO/gRPC синхронизация | Proto соответствует C# классу |

### Frontend (Next.js + Jest/RTL)

| Тип | Когда | Пример |
|-----|-------|--------|
| Unit | Чистые функции | `getTermClass(status)` |
| Component | React компоненты | `TermInspector` рендерит поля |
| Integration | API + UI | Сохранение term обновляет UI |

## Написание тестов

### xUnit (Backend)

```csharp
// Факт — инвариантное поведение
[Fact]
public void NormalizeText_TrimsAndLowercases()
{
    var result = TermService.NormalizeText("  Hello  ");
    Assert.Equal("hello", result);
}

// Теория — параметризованный тест
[Theory]
[InlineData("sleep", "slept")]
[InlineData("go", "went")]
[InlineData("take", "took")]
public void DifferentForms_AreNotDuplicates(string form1, string form2)
{
    var norm1 = TermService.NormalizeText(form1);
    var norm2 = TermService.NormalizeText(form2);
    Assert.NotEqual(norm1, norm2);
}

// Теория с данными из свойства
public static IEnumerable<object[]> DuplicateTestData =>
    new[]
    {
        new object[] { "hello", "hello", true },
        new object[] { "Hello", "HELLO", true },
        new object[] { "sleep", "slept", false },
    };

[Theory]
[MemberData(nameof(DuplicateTestData))]
public void IsDuplicate_DetectsExactMatches(
    string text1, string text2, bool expected)
{
    var result = _service.IsDuplicate(text1, text2);
    Assert.Equal(expected, result);
}
```

### Jest + React Testing Library (Frontend)

```typescript
// Unit test для чистой функции
describe('getTermClass', () => {
  it('returns blue class for NEW status', () => {
    expect(getTermClass('NEW')).toBe('text-blue-400');
  });
  
  it('returns yellow class for LINGQ status', () => {
    expect(getTermClass('LINGQ')).toBe('text-yellow-400');
  });
});

// Component test
describe('TermInspector', () => {
  it('renders term text and meaning input', () => {
    render(<TermInspector term={mockTerm} />);
    
    expect(screen.getByText(mockTerm.text)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Meaning...')).toBeInTheDocument();
  });
  
  it('calls onSave when Create LingQ clicked', async () => {
    const onSave = jest.fn();
    render(<TermInspector term={mockTerm} onSave={onSave} />);
    
    await userEvent.type(
      screen.getByPlaceholderText('Meaning...'), 
      'translation'
    );
    await userEvent.click(screen.getByText('Create LingQ'));
    
    expect(onSave).toHaveBeenCalledWith({
      id: mockTerm.id,
      meaning: 'translation',
    });
  });
});

// Integration test с MSW (Mock Service Worker)
describe('Reader page', () => {
  it('loads and displays text with terms', async () => {
    render(<ReaderPage params={{ id: '1' }} />);
    
    await waitFor(() => {
      expect(screen.getByText('Sample text')).toBeInTheDocument();
    });
    
    expect(screen.getByText('hello')).toHaveClass('text-blue-400');
  });
});
```

### Integration Tests с WebApplicationFactory

```csharp
public class TermsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    
    public TermsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace with test database
                services.AddDbContext<VocabularyDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateTerm_Returns201_WithValidRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/terms", 
            new { text = "hello", projectId = 1 });
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task GetTerm_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/terms/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

## LingQ Regression Tests

Обязательные тесты для Reader/Vocabulary изменений:

```csharp
[Fact]
public async Task Sleep_And_Slept_Have_Separate_Statuses()
{
    // Arrange
    var projectId = 1;
    var userId = 1;
    
    // Act
    await _service.CreateOrUpdateStatus("sleep", projectId, userId, TermStatus.Lingq);
    await _service.CreateOrUpdateStatus("slept", projectId, userId, TermStatus.Known);
    
    // Assert
    var sleepStatus = await _service.GetStatus("sleep", projectId, userId);
    var sleptStatus = await _service.GetStatus("slept", projectId, userId);
    
    sleepStatus.Should().Be(TermStatus.Lingq);
    sleptStatus.Should().Be(TermStatus.Known);
}

[Fact]
public async Task ExactPhrase_IsDuplicate_WordComponents_AreNot()
{
    // "take off" как фраза — дубль самой себя
    var phrase1 = await _service.CreateTerm("take off", projectId: 1);
    var phrase2 = await _service.CreateTerm("take off", projectId: 1);
    phrase1.Id.Should().Be(phrase2.Id);
    
    // "take" и "off" отдельно — не дубль фразы
    var take = await _service.CreateTerm("take", projectId: 1);
    var off = await _service.CreateTerm("off", projectId: 1);
    take.Id.Should().NotBe(phrase1.Id);
    off.Id.Should().NotBe(phrase1.Id);
}

[Fact]
public async Task PageTurn_MarksBlueAsKnown_WhenSettingEnabled()
{
    // Arrange: настройка включена
    await _settings.SetMarkBlueAsKnown(userId, enabled: true);
    
    // Создаём NEW термины на странице
    var terms = await CreateTermsAsync(["new1", "new2"], status: TermStatus.New);
    
    // Act: перелистывание
    await _readerService.OnPageTurn(pageId, userId);
    
    // Assert: термины стали KNOWN
    foreach (var term in terms)
    {
        var status = await _service.GetStatus(term.Text, projectId, userId);
        status.Should().Be(TermStatus.Known);
    }
}
```

## Запуск тестов

### Backend

```bash
# Все тесты
dotnet test

# Фильтр по имени
dotnet test --filter "FullyQualifiedName~TermService"
dotnet test --filter "FullyQualifiedName~Reader"

# Подробный вывод
dotnet test --verbosity normal

# Сборка перед тестами
dotnet test --no-build
```

### Frontend

```bash
# Все тесты
npm test

# Однократный запуск
npm test -- --watchAll=false

# Фильтр по пути
npm test -- --testPathPattern=reader

# Coverage
npm test -- --coverage
```

## Рецепты

### Тест с БД (InMemory)

```csharp
var options = new DbContextOptionsBuilder<VocabularyDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

using var context = new VocabularyDbContext(options);
var service = new TermService(context, Mock.Of<ILogger<TermService>>(), mapper);
```

### Мок зависимостей (Moq)

```csharp
var mockRepo = new Mock<IRepository<ProjectTerm>>();
mockRepo.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new ProjectTerm { Id = 1, Text = "hello" });

var service = new TermService(mockRepo.Object, logger, mapper);
```

### Async тест с таймаутом

```csharp
[Fact(Timeout = 5000)]  // 5 секунд максимум
public async Task LongRunningOperation_Completes()
{
    await _service.LongRunningAsync();
}
```

## Команды

- `.cursor/commands/tdd-start.md` — начало с failing test
- `.cursor/commands/tdd-verify.md` — проверка покрытия
