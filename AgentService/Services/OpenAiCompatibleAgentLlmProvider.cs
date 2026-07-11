using AgentService.Dtos.Agent;
using AgentService.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentService.Services;

public record LlmToolCall(string Id, string Name, string Arguments);

public record LlmCompletionResult(string Content, IReadOnlyList<LlmToolCall> ToolCalls);

public record AgentToolDefinition(string Name, string Description, object Parameters);

public interface IAgentLlmProvider
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);

    Task<LlmCompletionResult> CompleteChatAsync(
        string systemPrompt,
        IReadOnlyList<AgentChatMessageDto> messages,
        IEnumerable<AgentToolDefinition>? tools = null,
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

    public async Task<LlmCompletionResult> CompleteChatAsync(
        string systemPrompt,
        IReadOnlyList<AgentChatMessageDto> messages,
        IEnumerable<AgentToolDefinition>? tools = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AI completion is not configured");

        var messageList = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var message in messages)
        {
            if (message.Role.ToLowerInvariant() == "tool")
            {
                // Native OpenAI expects tool_call_id and name for tool responses
                // Our frontend currently doesn't send these fields, so we need to mock it if necessary.
                // Or better, just format tool responses as system/user messages for the LLM context.
                messageList.Add(new { role = "user", content = $"[Tool Response for {message.Content}]:\n{message.Content}" });
            }
            else
            {
                messageList.Add(new { role = MapRole(message.Role), content = message.Content });
            }
        }

        var payload = new Dictionary<string, object>
        {
            ["model"] = _options.Model,
            ["messages"] = messageList,
            ["stream"] = false
        };

        var toolList = tools?.ToList();
        if (toolList is { Count: > 0 })
        {
            payload["tools"] = toolList.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.Parameters
                }
            }).ToArray();
        }

        var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, jsonOptions), Encoding.UTF8, "application/json")
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
        var messageElement = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
        
        string content = string.Empty;
        if (messageElement.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
        {
            content = contentProp.GetString()?.Trim() ?? string.Empty;
        }

        var toolCalls = new List<LlmToolCall>();
        if (messageElement.TryGetProperty("tool_calls", out var toolCallsProp) && toolCallsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var tc in toolCallsProp.EnumerateArray())
            {
                if (tc.GetProperty("type").GetString() == "function")
                {
                    var func = tc.GetProperty("function");
                    toolCalls.Add(new LlmToolCall(
                        tc.GetProperty("id").GetString() ?? "",
                        func.GetProperty("name").GetString() ?? "",
                        func.GetProperty("arguments").GetString() ?? "{}"
                    ));
                }
            }
        }

        return new LlmCompletionResult(content, toolCalls);
    }

    private static string MapRole(string role) => role.ToLowerInvariant() switch
    {
        "assistant" => "assistant",
        "system" => "system",
        _ => "user"
    };
}
