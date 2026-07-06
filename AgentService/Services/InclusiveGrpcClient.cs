using Vocab;

namespace AgentService.Services;

public interface IInclusiveGrpcClient
{
    Task<AnalyzeTextResponse?> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default);

    Task<AnalyzeTargetWordResponse?> AnalyzeTargetWordAsync(
        string sentence,
        string targetWord,
        CancellationToken cancellationToken = default);
}

public class InclusiveGrpcClient : IInclusiveGrpcClient
{
    private readonly Vocab.VocabService.VocabServiceClient _client;
    private readonly ILogger<InclusiveGrpcClient> _logger;

    public InclusiveGrpcClient(Vocab.VocabService.VocabServiceClient client, ILogger<InclusiveGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<AnalyzeTextResponse?> AnalyzeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _client.AnalyzeTextAsync(
                new AnalyzeTextRequest { Text = text },
                cancellationToken: cancellationToken).ResponseAsync;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inclusive AnalyzeText failed for text length {Length}", text.Length);
            return null;
        }
    }

    public async Task<AnalyzeTargetWordResponse?> AnalyzeTargetWordAsync(
        string sentence,
        string targetWord,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _client.AnalyzeTargetWordAsync(
                new AnalyzeTargetWordRequest { Sentence = sentence, TargetWord = targetWord },
                cancellationToken: cancellationToken).ResponseAsync;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inclusive AnalyzeTargetWord failed for target word {TargetWord}", targetWord);
            return null;
        }
    }
}
