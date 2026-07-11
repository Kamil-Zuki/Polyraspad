using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentService.Orchestration;

public record AgentActionCard(
    string Id,
    string Title,
    string Kind,
    string Href,
    string Label,
    string? Description = null,
    Dictionary<string, string>? EditorDraft = null);

public record AgentExecutionResult(
    string AssistantContent,
    AgentDomainDecision DomainDecision,
    IReadOnlyList<AgentToolCallRecord> ToolCalls,
    bool IsError = false,
    string? IntentCategory = null,
    bool Refusal = false,
    IReadOnlyList<string>? SuggestedPrompts = null,
    IReadOnlyList<AgentActionCard>? Actions = null);

public record AgentToolCallRecord(
    string ToolName,
    string InputJson,
    string OutputJson,
    string Status);

public static class AgentMessageMetadataBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string? Build(AgentExecutionResult result)
    {
        var metadata = new Dictionary<string, object?>();

        if (result.Actions is { Count: > 0 })
            metadata["actions"] = result.Actions;

        if (result.IsError)
            metadata["isError"] = true;

        if (!string.IsNullOrEmpty(result.IntentCategory))
            metadata["intentCategory"] = result.IntentCategory;

        if (result.Refusal)
            metadata["refusal"] = true;

        if (result.SuggestedPrompts is { Count: > 0 })
            metadata["suggestedPrompts"] = result.SuggestedPrompts;

        if (result.ToolCalls is { Count: > 0 })
        {
            metadata["toolCalls"] = result.ToolCalls.Select(tc => new
            {
                name = tc.ToolName,
                status = tc.Status,
                result = tc.OutputJson
            }).ToArray();
        }

        return metadata.Count == 0 ? null : JsonSerializer.Serialize(metadata, JsonOptions);
    }

    public static AgentToolCallRecord BuildToolCallRecord(
        RoutedAgentIntent intent,
        string userText,
        AgentExecutionResult result)
    {
        var input = JsonSerializer.Serialize(new
        {
            userText,
            word = intent.Word,
            sentence = intent.Sentence,
            destination = intent.Destination?.ToString().ToLowerInvariant()
        }, JsonOptions);

        var output = JsonSerializer.Serialize(new
        {
            content = result.AssistantContent,
            actions = result.Actions,
            isError = result.IsError,
            intentCategory = result.IntentCategory ?? result.DomainDecision.CategoryName,
            refusal = result.Refusal,
            suggestedPrompts = result.SuggestedPrompts
        }, JsonOptions);

        return new AgentToolCallRecord(
            intent.ToolName,
            input,
            output,
            result.IsError ? "error" : "completed");
    }
}
