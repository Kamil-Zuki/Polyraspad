using Grpc.Core;
using Pvs.Content.Grpc;
using static Pvs.Content.Grpc.AnalyticsService;
using static Pvs.Content.Grpc.AIService;

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
}

public class VocabularyGrpcClient : IVocabularyGrpcClient
{
    private readonly AnalyticsServiceClient _analyticsClient;
    private readonly AIServiceClient _aiClient;

    public VocabularyGrpcClient(AnalyticsServiceClient analyticsClient, AIServiceClient aiClient)
    {
        _analyticsClient = analyticsClient;
        _aiClient = aiClient;
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

    private static Metadata BuildMetadata(Guid userId, IEnumerable<string> roles) => new()
    {
        { "user_id", userId.ToString() },
        { "roles", string.Join(",", roles) }
    };
}
