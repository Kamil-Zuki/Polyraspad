# TDD Test Matrix for Reader/Library

**Стратегия:** Unit → Integration → E2E по мере увеличения риска

## Backend Tests (.NET + xUnit)

### Unit Tests

#### TextService Tests
```csharp
[Fact]
public void Tokenize_ExtractsWordsFromText()

[Fact]
public void Tokenize_HandlesPunctuationCorrectly()

[Fact]
public void Tokenize_RespectsUnicode()

[Theory]
[InlineData("hello", "hello")]
[InlineData("  Hello  ", "hello")]
[InlineData("HELLO", "hello")]
public void NormalizeText_ReturnsLowercaseTrimmed(string input, string expected)

[Fact]
public void LoadTermStatuses_AssignsCorrectStatusToTokens()

[Fact]
public void LoadTermStatuses_HandlesPhrasesWithPriority()
```

#### TermService Tests
```csharp
[Fact]
public void CreateTerm_SavesExactFormWithoutLemmatization()

[Theory]
[InlineData("sleep", "slept")]
[InlineData("go", "went")]
[InlineData("take", "took")]
public void DifferentForms_AreNotDuplicates(string form1, string form2)

[Fact]
public void CreatePhrase_CreatesTermWithPhraseType()

[Fact]
public void CreatePhrase_HandlesMaxLength()

[Fact]
public void MarkKnown_UpdatesStatusToKnown()

[Fact]
public void BulkMarkKnown_UpdatesAllProvidedTerms()

[Fact]
public void SearchDuplicates_FindsExactMatch()

[Fact]
public void SearchDuplicates_DoesNotFindComponentWordsAsDuplicates()
```

### Integration Tests

#### Text Analysis API
```csharp
[Fact]
public async Task AnalyzeText_Returns200_WithValidRequest()

[Fact]
public async Task AnalyzeText_Returns429_WhenRateLimited()

[Fact]
public async Task AnalyzeText_Returns400_ForTooLongText()

[Fact]
public async Task AnalyzeText_IncludesPhraseStatusesInResponse()
```

#### Term Operations API
```csharp
[Fact]
public async Task CreateTerm_Returns201_CreatesNewTerm()

[Fact]
public async Task CreateTerm_Returns200_UpdatesExistingTerm()

[Fact]
public async Task MarkKnown_Returns200_UpdatesStatus()

[Fact]
public async Task BulkMarkKnown_Returns200_UpdatesMultiple()

[Fact]
public async Task BulkMarkKnown_HandlesPartialFailures()

[Fact]
public async Task SearchDuplicates_ReturnsExactAndSimilar()
```

#### End-to-End Flow
```csharp
[Fact]
public async Task FullFlow_CreateLingQ_VerifyInAnalysis()

[Fact]
public async Task FullFlow_MarkKnown_BulkVerify()

[Fact]
public async Task FullFlow_CreatePhrase_VerifyHighlightPriority()
```

## Frontend Tests (Jest + React Testing Library)

### Unit Tests

#### Reader Utils
```typescript
describe('getTokenStatusClass', () => {
  it('returns blue for NEW status')
  it('returns yellow for LINGQ status')
  it('returns white for KNOWN status')
  it('returns muted for IGNORED status')
})

describe('normalizeText', () => {
  it('trims and lowercases input')
  it('handles unicode correctly')
})

describe('extractSentenceFromTokens', () => {
  it('extracts sentence containing token index')
  it('handles boundary conditions')
})
```

#### Token Rendering
```typescript
describe('renderTokens', () => {
  it('renders words with correct status classes')
  it('prioritizes phrases over individual words')
  it('handles overlapping phrase and word')
})
```

### Component Tests

#### Reader Component
```typescript
describe('Reader', () => {
  it('renders text with highlighted tokens')
  it('opens inspector on word click')
  it('closes inspector on escape key')
  it('updates token color after LingQ creation')
  it('shows loading state during analysis')
  it('displays stats correctly')
})
```

#### TermInspector Component
```typescript
describe('TermInspector', () => {
  it('displays selected word and sentence')
  it('shows translation when loaded')
  it('calls onCreateLingQ with meaning')
  it('calls onMarkKnown on button click')
  it('calls onIgnore on button click')
  it('displays duplicate cards')
})
```

#### Phrase Selection
```typescript
describe('PhraseSelection', () => {
  it('starts selection on shift-click')
  it('extends selection on second shift-click')
  it('limits max phrase length')
  it('cancels on escape')
  it('creates phrase on confirm')
})
```

### Integration Tests

#### API Integration
```typescript
describe('TextClient', () => {
  it('analyzes text and returns tokens')
  it('handles network errors gracefully')
  it('retries on transient failures')
})

describe('TermClient', () => {
  it('creates term and returns details')
  it('marks term as known')
  it('performs bulk known operation')
  it('searches for duplicates')
})
```

#### State Management
```typescript
describe('Reader State', () => {
  it('maintains token states across re-renders')
  it('syncs term updates with analysis')
  it('handles page turn with bulk update')
})
```

## E2E Tests (Playwright)

### Critical User Paths

#### Path 1: Create First LingQ
```
1. Open /reader
2. Paste text "The quick brown fox"
3. Click "Analyze"
4. Click on "quick"
5. Enter meaning "быстрый"
6. Click "Create LingQ"
7. Verify word turns yellow
```

#### Path 2: Phrase LingQ
```
1. Open text in reader
2. Shift+click "quick"
3. Shift+click "fox"
4. Verify phrase selected
5. Enter meaning
6. Save
7. Verify phrase highlighted as unit
```

#### Path 3: Page Turn Bulk Known
```
1. Open text
2. Note blue words count
3. Enable "Mark as known on page turn"
4. Click next page
5. Verify previous page words are now white
```

#### Path 4: Review from Reader
```
1. Open reader with saved LingQs
2. Wait for review count > 0
3. Click "Review: N"
4. Complete 3 cards
5. Click back
6. Verify return to same position
```

#### Path 5: Continue Reading
```
1. Open book, go to page 10
2. Close reader
3. Go to dashboard
4. Click "Continue Reading"
5. Verify opened at page 10
```

## Regression Test Suite

### Form Distinction (Обязательно)
```typescript
[Fact]
public async Task Sleep_And_Slept_HaveDifferentStatuses()
{
    // Arrange
    var projectId = await CreateProjectAsync();
    
    // Act
    await _termService.CreateOrUpdateAsync("sleep", projectId, userId, LINGQ);
    await _termService.CreateOrUpdateAsync("slept", projectId, userId, KNOWN);
    
    // Assert
    var sleepStatus = await _termService.GetStatusAsync("sleep", projectId, userId);
    var sleptStatus = await _termService.GetStatusAsync("slept", projectId, userId);
    
    Assert.Equal(LINGQ, sleepStatus);
    Assert.Equal(KNOWN, sleptStatus);
    Assert.NotEqual(sleepStatus, sleptStatus);
}
```

### Phrase Handling
```typescript
[Fact]
public async Task Phrase_IsSeparateFromComponentWords()
{
    // Arrange
    var projectId = await CreateProjectAsync();
    
    // Act - create phrase
    var phrase = await _termService.CreatePhraseAsync(["take", "off"], projectId, userId);
    
    // Create individual words
    var take = await _termService.CreateOrUpdateAsync("take", projectId, userId, KNOWN);
    var off = await _termService.CreateOrUpdateAsync("off", projectId, userId, KNOWN);
    
    // Assert
    Assert.NotEqual(phrase.Id, take.Id);
    Assert.NotEqual(phrase.Id, off.Id);
    
    // Phrase status is independent
    var phraseStatus = await _termService.GetStatusAsync("take off", projectId, userId);
    Assert.Equal(NEW, phraseStatus); // Not affected by component words
}
```

### Duplicate Detection
```typescript
[Fact]
public async Task ExactDuplicate_Found_DifferentForm_NotDuplicate()
{
    // Arrange
    var projectId = await CreateProjectAsync();
    await _termService.CreateOrUpdateAsync("sleep", projectId, userId, LINGQ);
    
    // Act & Assert
    var exactDup = await _termService.SearchDuplicatesAsync("sleep", projectId);
    Assert.Single(exactDup.ExactMatches);
    
    var formDup = await _termService.SearchDuplicatesAsync("slept", projectId);
    Assert.Empty(formDup.ExactMatches);
}
```

### Page Turn Behavior
```typescript
[Fact]
public async Task PageTurn_WithSettingEnabled_MarksAsKnown()
{
    // Arrange
    var projectId = await CreateProjectAsync();
    await _settings.SetMarkBlueAsKnownAsync(userId, enabled: true);
    
    var newTerms = await CreateTermsAsync(["new1", "new2"], projectId, NEW);
    
    // Act
    await _readerService.OnPageTurnAsync(pageId, userId, projectId);
    
    // Assert
    foreach (var term in newTerms)
    {
        var status = await _termService.GetStatusAsync(term, projectId, userId);
        Assert.Equal(KNOWN, status);
    }
}

[Fact]
public async Task PageTurn_WithSettingDisabled_KeepsAsNew()
{
    // Arrange
    var projectId = await CreateProjectAsync();
    await _settings.SetMarkBlueAsKnownAsync(userId, enabled: false);
    
    var newTerms = await CreateTermsAsync(["new1", "new2"], projectId, NEW);
    
    // Act
    await _readerService.OnPageTurnAsync(pageId, userId, projectId);
    
    // Assert
    foreach (var term in newTerms)
    {
        var status = await _termService.GetStatusAsync(term, projectId, userId);
        Assert.Equal(NEW, status);
    }
}
```

## Test Data Fixtures

### Sample Texts
```
EN_SIMPLE: "The quick brown fox jumps over the lazy dog."
EN_COMPLEX: "Despite the arduous journey, the intrepid explorer persevered..."
RU_SIMPLE: "Быстрая коричневая лисица прыгает через ленивую собаку."
```

### Test Term Sets
```
FORMS_TEST: ["sleep", "slept", "sleeping"]
PHRASE_TEST: ["take off", "look forward to", "give up"]
DUPLICATE_TEST: ["hello", "Hello", "HELLO"]
```

## CI/CD Integration

```yaml
# GitHub Actions example
test:
  steps:
    - name: Unit Tests
      run: dotnet test --filter "FullyQualifiedName~Unit"
      
    - name: Integration Tests
      run: dotnet test --filter "FullyQualifiedName~Integration"
      
    - name: Frontend Tests
      run: npm test -- --coverage
      
    - name: E2E Tests
      run: npx playwright test
      env:
        CI: true
```

## Coverage Targets

| Layer | Target | Critical Files |
|-------|--------|----------------|
| Backend Unit | 80% | TermService, TextService |
| Backend Integration | 70% | All Controllers |
| Frontend Unit | 75% | Reader components, hooks |
| E2E | Critical paths | 5 main user flows |
