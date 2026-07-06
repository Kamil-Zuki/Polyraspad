using AgentService.Dtos.Agent;
using AgentService.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AgentService.Services;

public interface IAgentLlmProvider
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);

    Task<string> CompleteChatAsync(
        string systemPrompt,
        IReadOnlyList<AgentChatMessageDto> messages,
        CancellationToken cancellationToken = default);
}

public class OpenAiCompatibleAgentLlmProvider : IAgentLlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiCompatibleAgentLlmProvider> _logger;

    public OpenAiCompatibleAgentLlmProvider(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OpenAiCompatibleAgentLlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AI completion is not configured");

        var payload = new
        {
            model = _options.Model,
            messages = new[] { new { role = "user", content = prompt } },
            stream = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("LLM request failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException("AI completion request failed");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? string.Empty;
    }

    public async Task<string> CompleteChatAsync(
        string systemPrompt,
        IReadOnlyList<AgentChatMessageDto> messages,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AI completion is not configured");

        var messageList = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var message in messages)
        {
            messageList.Add(new { role = MapRole(message.Role), content = message.Content });
        }

        var payload = new
        {
            model = _options.Model,
            messages = messageList,
            stream = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("LLM chat request failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException("AI completion request failed");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? string.Empty;
    }

    private static string MapRole(string role) => role.ToLowerInvariant() switch
    {
        "assistant" => "assistant",
        "system" => "system",
        "tool" => "user",
        _ => "user"
    };
}
