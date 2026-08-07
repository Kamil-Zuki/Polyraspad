using AgentService.Dtos.Agent;
using AgentService.Orchestration;
using AgentService.Options;
using Microsoft.Extensions.Options;
using Pvs.Content.Grpc;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentService.Infrastructure;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0110

namespace AgentService.Services;

public interface IAgentOrchestrator
{
    Task<CreateAgentRunResultDto?> ExecuteRunAsync(
        Guid userId,
        Guid threadId,
        Guid projectId,
        ExecuteAgentRunDto request,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ExecuteRunStreamEvent> ExecuteRunStreamAsync(
        Guid userId,
        Guid threadId,
        Guid projectId,
        ExecuteAgentRunDto request,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}

public record ExecuteRunStreamEvent
{
    public string? ContentChunk { get; init; }
    public AgentToolCallRecord? ToolCall { get; init; }
    public CreateAgentRunResultDto? FinalResult { get; init; }
    public string? Error { get; init; }
}

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IAgentThreadService _threadService;
    private readonly IVocabularyProjectAccessValidator _projectAccessValidator;
    private readonly IVocabularyGrpcClient _vocabularyClient;
    private readonly AgentKernelFactory _kernelFactory;
    private readonly AiOptions _aiOptions;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IAgentThreadService threadService,
        IVocabularyProjectAccessValidator projectAccessValidator,
        IVocabularyGrpcClient vocabularyClient,
        AgentKernelFactory kernelFactory,
        IOptions<AiOptions> aiOptions,
        ILogger<AgentOrchestrator> logger)
    {
        _threadService = threadService;
        _projectAccessValidator = projectAccessValidator;
        _vocabularyClient = vocabularyClient;
        _kernelFactory = kernelFactory;
        _aiOptions = aiOptions.Value;
        _logger = logger;
    }

    public async Task<CreateAgentRunResultDto?> ExecuteRunAsync(
        Guid userId,
        Guid threadId,
        Guid projectId,
        ExecuteAgentRunDto request,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var stream = ExecuteRunStreamAsync(userId, threadId, projectId, request, roles, cancellationToken);
        CreateAgentRunResultDto? finalResult = null;
        
        await foreach (var evt in stream)
        {
            if (evt.FinalResult != null)
            {
                finalResult = evt.FinalResult;
            }
        }

        return finalResult;
    }

    public async IAsyncEnumerable<ExecuteRunStreamEvent> ExecuteRunStreamAsync(
        Guid userId,
        Guid threadId,
        Guid projectId,
        ExecuteAgentRunDto request,
        IEnumerable<string> roles,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserText))
            throw new ArgumentException("User text is required");

        var project = await _projectAccessValidator.EnsureProjectAccessAsync(
            userId, projectId, roles, cancellationToken);

        var sourceLang = request.SourceLang ?? project.SourceLang;
        var targetLang = request.TargetLang ?? project.TargetLang;
        
        var thread = await _threadService.GetThreadAsync(userId, threadId, cancellationToken);
        var agentId = thread?.AgentId ?? "study-copilot";

        var systemPrompt = !string.IsNullOrWhiteSpace(thread?.SystemPromptOverride) 
            ? thread.SystemPromptOverride 
            : AgentSystemPromptBuilder.Build(agentId, project.Title, sourceLang, targetLang);

        var intent = AgentIntentRouter.Route(request.UserText);
        
        if (!intent.Domain!.Allowed)
        {
            var refusal = AgentDomainPolicy.BuildOutOfScopeRefusal(request.UserText, sourceLang);
            var refusedRunResult = await _threadService.CreateRunAsync(userId, threadId, projectId, new CreateAgentRunDto
            {
                UserMessage = new AgentMessageInputDto { Role = "user", Content = request.UserText.Trim() },
                AssistantMessage = new AgentMessageInputDto
                {
                    Role = "assistant",
                    Content = refusal,
                    MetadataJson = AgentMessageMetadataBuilder.Build(new AgentExecutionResult(
                        AssistantContent: refusal,
                        DomainDecision: intent.Domain,
                        ToolCalls: [],
                        IntentCategory: intent.Domain.CategoryName,
                        Refusal: true
                    ))
                },
                DomainDecision = new AgentDomainDecisionInputDto
                {
                    Allowed = intent.Domain.Allowed,
                    Category = intent.Domain.CategoryName,
                    Reason = intent.Domain.Reason
                },
                ToolCalls = [],
                Model = null
            }, cancellationToken);

            yield return new ExecuteRunStreamEvent { ContentChunk = refusal };
            yield return new ExecuteRunStreamEvent { FinalResult = refusedRunResult };
            yield break;
        }
        
        // Setup Kernel & Agent
        var kernel = _kernelFactory.CreateKernel(_vocabularyClient, userId, projectId, roles);
        var agent = new ChatCompletionAgent
        {
            Name = agentId,
            Instructions = systemPrompt,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings 
                { 
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
                })
        };

        // Load History
        var historyMessages = await LoadHistoryAsync(userId, threadId, cancellationToken);
        var chatHistory = new ChatHistory();
        foreach (var m in historyMessages)
        {
            chatHistory.AddMessage(m.Role == "user" ? AuthorRole.User : AuthorRole.Assistant, m.Content);
        }

        var agentThread = new ChatHistoryAgentThread(chatHistory);
        var userMessage = new ChatMessageContent(AuthorRole.User, request.UserText);
        
        var executedTools = new List<AgentToolCallRecord>();
        var assistantContentBuilder = new System.Text.StringBuilder();

        await foreach (var responseItem in agent.InvokeStreamingAsync(userMessage, agentThread, cancellationToken: cancellationToken))
        {
            // Extract the StreamingChatMessageContent from AgentResponseItem
            // For now, we'll try casting or getting a Content property dynamically to avoid property mismatches.
            dynamic itemDyn = responseItem;
            string? chunkContent = null;
            try {
                // If it's AgentResponseItem<StreamingChatMessageContent> we can get Value.Content
                chunkContent = itemDyn.Value?.Content;
            } catch {
                try { chunkContent = itemDyn.Content; } catch {}
            }

            if (chunkContent != null)
            {
                assistantContentBuilder.Append(chunkContent);
                yield return new ExecuteRunStreamEvent { ContentChunk = chunkContent };
            }
            
            // Note: In InvokeStreamingAsync, tool calls might be reported in different chunks depending on the provider.
            // Semantic Kernel currently wraps tool calls execution seamlessly in Auto mode,
            // but we want to capture the executed tools for our persistence layer.
            // The actual tool calls are executed synchronously/asynchronously inside SK's pipeline when Auto is set.
            // We can retrieve them from the final response items or we might need to rely on agent.InvokeAsync if we want full manual interception,
            // but SK handles it via hooks/filters.
        }

        var fullHistory = await agentThread.GetMessagesAsync(cancellationToken).ToArrayAsync();
        // Extract tool calls from the history to persist them
        // In Semantic Kernel, tool calls are recorded as ToolCall messages in the history.
        var actions = new List<AgentActionCard>();
        foreach (var msg in fullHistory)
        {
            // msg.Items contains text content, function calls, function results
            foreach (var item in msg.Items)
            {
                if (item is FunctionCallContent fcc)
                {
                    // This is a function call request
                }
                else if (item is FunctionResultContent frc)
                {
                    // This is a function result
                    var resultStr = frc.Result?.ToString() ?? "{}";
                    executedTools.Add(new AgentToolCallRecord(frc.PluginName + "-" + frc.FunctionName, "", resultStr, "completed"));

                    // Extract UI actions
                    try
                    {
                        var jDoc = JsonDocument.Parse(resultStr);
                        if (jDoc.RootElement.TryGetProperty("actionType", out var actionTypeProp))
                        {
                            var actionType = actionTypeProp.GetString();
                            if (actionType == "navigate")
                            {
                                actions.Add(new AgentActionCard(
                                    Guid.NewGuid().ToString(),
                                    jDoc.RootElement.GetProperty("label").GetString() ?? "Navigate",
                                    "navigate",
                                    jDoc.RootElement.GetProperty("destination").GetString() ?? "/",
                                    "Open",
                                    jDoc.RootElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null));
                            }
                            else if (actionType == "open_editor_draft")
                            {
                                var payload = jDoc.RootElement.GetProperty("payload");
                                var draft = new Dictionary<string, string>();
                                if (payload.TryGetProperty("word", out var wordProp)) draft["word"] = wordProp.GetString() ?? "";
                                if (payload.TryGetProperty("expression", out var exprProp)) draft["expression"] = exprProp.GetString() ?? "";
                                if (payload.TryGetProperty("translation", out var transProp)) draft["translation"] = transProp.GetString() ?? "";

                                actions.Add(new AgentActionCard(
                                    Guid.NewGuid().ToString(),
                                    jDoc.RootElement.GetProperty("label").GetString() ?? "Draft Card",
                                    "open_editor_draft",
                                    jDoc.RootElement.GetProperty("destination").GetString() ?? "/editor",
                                    "Open Editor",
                                    jDoc.RootElement.TryGetProperty("description", out var dProp) ? dProp.GetString() : null,
                                    draft));
                            }
                        }
                    }
                    catch { } // Ignore JSON parse errors for non-action results
                }
            }
        }

        var assistantContent = assistantContentBuilder.ToString();
        if (string.IsNullOrWhiteSpace(assistantContent) && executedTools.Count > 0)
        {
            assistantContent = "Я успешно выполнил запрошенные действия.";
        }

        var execution = new AgentExecutionResult(
            assistantContent,
            new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning),
            executedTools,
            IntentCategory: "language_learning",
            Actions: actions.Count > 0 ? actions : null);

        var effectiveDomain = intent;

        var finalResult = await _threadService.CreateRunAsync(userId, threadId, projectId, new CreateAgentRunDto
        {
            UserMessage = new AgentMessageInputDto { Role = "user", Content = request.UserText.Trim() },
            AssistantMessage = new AgentMessageInputDto
            {
                Role = "assistant",
                Content = execution.AssistantContent,
                MetadataJson = AgentMessageMetadataBuilder.Build(execution)
            },
            DomainDecision = new AgentDomainDecisionInputDto
            {
                Allowed = effectiveDomain.Domain!.Allowed,
                Category = effectiveDomain.Domain.CategoryName,
                Reason = effectiveDomain.Domain.Reason
            },
            ToolCalls = executedTools.Select(toolCall => new AgentToolCallInputDto
            {
                ToolName = toolCall.ToolName,
                InputJson = toolCall.InputJson,
                OutputJson = toolCall.OutputJson,
                Status = toolCall.Status
            }).ToList(),
            Model = _aiOptions.Enabled ? _aiOptions.Model : null
        }, cancellationToken);

        yield return new ExecuteRunStreamEvent { FinalResult = finalResult };
    }

    private async Task<IReadOnlyList<AgentChatMessageDto>> LoadHistoryAsync(
        Guid userId,
        Guid threadId,
        CancellationToken cancellationToken)
    {
        const int historyLimit = 10;
        var list = await _threadService.ListMessagesAsync(userId, threadId, historyLimit, null, cancellationToken);
        if (list is null) return Array.Empty<AgentChatMessageDto>();

        var validRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "user", "assistant" };
        
        return list.Items
            .Where(m => validRoles.Contains(m.Role.ToLowerInvariant()))
            .Select(m => new AgentChatMessageDto(m.Role.ToLowerInvariant(), m.Content))
            .ToList();
    }
}
