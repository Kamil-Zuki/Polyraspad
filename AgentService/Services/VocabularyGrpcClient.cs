using Grpc.Core;
using Pvs.Content.Grpc;
using static Pvs.Content.Grpc.AnalyticsService;
using static Pvs.Content.Grpc.AIService;
using static Pvs.Content.Grpc.ContentService;
using static Pvs.Content.Grpc.CardService;
using static Pvs.Content.Grpc.LessonService;
using static Pvs.Content.Grpc.TermService;

namespace AgentService.Services;

public interface IVocabularyGrpcClient
{
    Task<GetVocabularyStatsResponse> GetVocabularyStatsAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetDailySummaryResponse> GetDailySummaryAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<ExplainGrammarResponse> ExplainGrammarAsync(
        Guid userId,
        string sentence,
        string targetWord,
        string nativeLanguage,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GenerateContextResponse> GenerateContextAsync(
        Guid userId,
        string targetWord,
        string language,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<DeckResponse> CreateDeckAsync(
        Guid userId,
        Guid projectId,
        string title,
        string? description,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<CardResponse> CreateCardAsync(
        Guid userId,
        Guid deckId,
        string word,
        string translation,
        string? expression,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetLeechCardsResponse> GetLeechCardsAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<GetDeckTreeResponse> GetDeckTreeAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task CompleteLessonAsync(
        Guid userId,
        Guid lessonId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetLearningTermsAsync(
        Guid userId,
        Guid projectId,
        int count,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}

public class VocabularyGrpcClient : IVocabularyGrpcClient
{
    private readonly AnalyticsServiceClient _analyticsClient;
    private readonly AIServiceClient _aiClient;
    private readonly ContentServiceClient _contentClient;
    private readonly CardServiceClient _cardClient;
    private readonly LessonServiceClient _lessonClient;
    private readonly TermServiceClient _termClient;

    public VocabularyGrpcClient(
        AnalyticsServiceClient analyticsClient, 
        AIServiceClient aiClient,
        ContentServiceClient contentClient,
        CardServiceClient cardClient,
        LessonServiceClient lessonClient,
        TermServiceClient termClient)
    {
        _analyticsClient = analyticsClient;
        _aiClient = aiClient;
        _contentClient = contentClient;
        _cardClient = cardClient;
        _lessonClient = lessonClient;
        _termClient = termClient;
    }

    public Task<GetVocabularyStatsResponse> GetVocabularyStatsAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return _analyticsClient.GetVocabularyStatsAsync(
            new GetVocabularyStatsRequest
            {
                UserId = userId.ToString(),
                ProjectId = projectId.ToString()
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<GetDailySummaryResponse> GetDailySummaryAsync(
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return _analyticsClient.GetDailySummaryAsync(
            new GetDailySummaryRequest { UserId = userId.ToString() },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<ExplainGrammarResponse> ExplainGrammarAsync(
        Guid userId,
        string sentence,
        string targetWord,
        string nativeLanguage,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return _aiClient.ExplainGrammarAsync(
            new ExplainGrammarRequest
            {
                UserId = userId.ToString(),
                Sentence = sentence,
                TargetWord = targetWord,
                UserNativeLanguage = nativeLanguage
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<GenerateContextResponse> GenerateContextAsync(
        Guid userId,
        string targetWord,
        string language,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return _aiClient.GenerateContextAsync(
            new GenerateContextRequest
            {
                UserId = userId.ToString(),
                TargetWord = targetWord,
                Language = language,
                UserLevel = "B1",
                Count = 1
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<DeckResponse> CreateDeckAsync(
        Guid userId,
        Guid projectId,
        string title,
        string? description,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return _contentClient.CreateDeckAsync(
            new CreateDeckRequest
            {
                UserId = userId.ToString(),
                ProjectId = projectId.ToString(),
                Title = title,
                Description = description,
                IsPublic = false
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<CardResponse> CreateCardAsync(
        Guid userId,
        Guid deckId,
        string word,
        string translation,
        string? expression,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, NoteFieldValuePayload>
        {
            { "Word", new NoteFieldValuePayload { StringValue = word } },
            { "Translation", new NoteFieldValuePayload { StringValue = translation } }
        };

        if (!string.IsNullOrWhiteSpace(expression))
        {
            fields["Expression"] = new NoteFieldValuePayload { StringValue = expression };
        }

        return _cardClient.CreateCardAsync(
            new CreateCardRequest
            {
                UserId = userId.ToString(),
                DeckId = deckId.ToString(),
                FieldValues = { fields }
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public Task<GetLeechCardsResponse> GetLeechCardsAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return _cardClient.GetLeechCardsAsync(
            new GetLeechCardsRequest
            {
                UserId = userId.ToString(),
                ProjectId = projectId.ToString(),
                PageSize = 20,
                PageNumber = 1
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public async Task<GetDeckTreeResponse> GetDeckTreeAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return await _contentClient.GetDeckTreeAsync(
            new GetDeckTreeRequest
            {
                UserId = userId.ToString(),
                ProjectId = projectId.ToString()
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken);
    }

    public async Task CompleteLessonAsync(
        Guid userId,
        Guid lessonId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        await _lessonClient.CompleteLessonAsync(
            new CompleteLessonRequest
            {
                UserId = userId.ToString(),
                LessonId = lessonId.ToString()
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetLearningTermsAsync(
        Guid userId,
        Guid projectId,
        int count,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var response = await _termClient.ListProjectTermsAsync(
            new ListProjectTermsRequest
            {
                UserId = userId.ToString(),
                ProjectId = projectId.ToString(),
                Status = "SAVED",
                PageSize = count
            },
            headers: BuildMetadata(userId, roles),
            cancellationToken: cancellationToken);

        return response.Items.Select(x => x.Text).ToList();
    }

    private static Metadata BuildMetadata(Guid userId, IEnumerable<string> roles) => new()
    {
        { "user_id", userId.ToString() },
        { "roles", string.Join(",", roles) }
    };
}
