using AgentService.Dtos.Agent;
using AgentService.Orchestration;
using AgentService.Options;
using Microsoft.Extensions.Options;
using Pvs.Content.Grpc;

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
}

public class AgentOrchestrator : IAgentOrchestrator
{
    private static readonly HashSet<AgentToolId> LlmToolIds =
    [
        AgentToolId.ExplainWord,
        AgentToolId.GrammarHelp,
        AgentToolId.GenerateExample,
        AgentToolId.BuildCardDraft,
        AgentToolId.GeneralAnswer
    ];

    private readonly IAgentThreadService _threadService;
    private readonly IVocabularyProjectAccessValidator _projectAccessValidator;
    private readonly IVocabularyGrpcClient _vocabularyClient;
    private readonly IAgentLlmProvider _llmProvider;
    private readonly AiOptions _aiOptions;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IAgentThreadService threadService,
        IVocabularyProjectAccessValidator projectAccessValidator,
        IVocabularyGrpcClient vocabularyClient,
        IAgentLlmProvider llmProvider,
        IOptions<AiOptions> aiOptions,
        ILogger<AgentOrchestrator> logger)
    {
        _threadService = threadService;
        _projectAccessValidator = projectAccessValidator;
        _vocabularyClient = vocabularyClient;
        _llmProvider = llmProvider;
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
        if (string.IsNullOrWhiteSpace(request.UserText))
            throw new ArgumentException("User text is required");

        var project = await _projectAccessValidator.EnsureProjectAccessAsync(
            userId, projectId, roles, cancellationToken);

        var sourceLang = request.SourceLang ?? project.SourceLang;
        var targetLang = request.TargetLang ?? project.TargetLang;
        var intent = AgentIntentRouter.Route(request.UserText);
        var domainDecision = AgentDomainPolicy.Classify(request.UserText);

        if (LlmToolIds.Contains(intent.ToolId) && !domainDecision.Allowed)
            intent = new RoutedAgentIntent(AgentToolId.OutOfScope, Domain: domainDecision);

        AgentExecutionResult execution;
        try
        {
            execution = await ExecuteToolAsync(
                intent,
                request.UserText,
                userId,
                projectId,
                project,
                sourceLang,
                targetLang,
                request.FirstDeckId,
                roles,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent tool execution failed for thread {ThreadId}", threadId);
            execution = new AgentExecutionResult(
                ex is InvalidOperationException ? ex.Message : "Something went wrong.",
                intent.Domain ?? domainDecision,
                Array.Empty<AgentToolCallRecord>(),
                IsError: true);
        }

        var effectiveDomain = intent.Domain ?? domainDecision;
        var toolCall = AgentMessageMetadataBuilder.BuildToolCallRecord(intent, request.UserText, execution);

        return await _threadService.CreateRunAsync(userId, threadId, projectId, new CreateAgentRunDto
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
                Allowed = effectiveDomain.Allowed,
                Category = effectiveDomain.CategoryName,
                Reason = effectiveDomain.Reason
            },
            ToolCalls =
            [
                new AgentToolCallInputDto
                {
                    ToolName = toolCall.ToolName,
                    InputJson = toolCall.InputJson,
                    OutputJson = toolCall.OutputJson,
                    Status = toolCall.Status
                }
            ],
            Model = _aiOptions.Enabled ? _aiOptions.Model : null
        }, cancellationToken);
    }

    private async Task<AgentExecutionResult> ExecuteToolAsync(
        RoutedAgentIntent intent,
        string userText,
        Guid userId,
        Guid projectId,
        ProjectResponse project,
        string sourceLang,
        string targetLang,
        string? firstDeckId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        return intent.ToolId switch
        {
            AgentToolId.Navigate => HandleNavigate(intent, firstDeckId),
            AgentToolId.GetProgress => await HandleProgressAsync(userId, projectId, project.Title, firstDeckId, roles, cancellationToken),
            AgentToolId.ExplainWord => await HandleExplainWordAsync(intent, userId, sourceLang, targetLang, firstDeckId, roles, cancellationToken),
            AgentToolId.GrammarHelp => await HandleGrammarHelpAsync(intent, userId, targetLang, roles, cancellationToken),
            AgentToolId.GenerateExample => await HandleGenerateExampleAsync(intent, userId, sourceLang, targetLang, roles, cancellationToken),
            AgentToolId.BuildCardDraft => await HandleBuildCardDraftAsync(intent, userId, sourceLang, targetLang, roles, cancellationToken),
            AgentToolId.OutOfScope => HandleOutOfScope(userText, sourceLang, intent),
            AgentToolId.GeneralAnswer => await HandleGeneralAnswerAsync(userText, project.Title, sourceLang, targetLang, cancellationToken),
            _ => new AgentExecutionResult(
                "I couldn't understand that request yet.",
                intent.Domain ?? AgentDomainPolicy.Classify(userText),
                Array.Empty<AgentToolCallRecord>(),
                IsError: true)
        };
    }

    private static AgentExecutionResult HandleNavigate(RoutedAgentIntent intent, string? firstDeckId)
    {
        var destination = intent.Destination ?? AgentNavigateDestination.Library;
        var action = BuildNavigateAction(destination, firstDeckId);
        return new AgentExecutionResult(
            $"Opening {action.Title}. You can continue there or ask me something else here.",
            intent.Domain!,
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "product_navigation",
            Actions: [action]);
    }

    private async Task<AgentExecutionResult> HandleProgressAsync(
        Guid userId,
        Guid projectId,
        string projectTitle,
        string? firstDeckId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var daily = await _vocabularyClient.GetDailySummaryAsync(userId, roles, cancellationToken);
        var vocab = await _vocabularyClient.GetVocabularyStatsAsync(userId, projectId, roles, cancellationToken);

        var streak = daily.CurrentStreak;
        var content = AgentIntentRouter.SanitizeLemmaLabels($"""
            Here's your progress for {projectTitle}:

            • Streak: {streak} day{(streak == 1 ? "" : "s")}
            • Reviews today: {daily.Reviews.Current} / {daily.Reviews.Target}{(daily.Reviews.IsCompleted ? " (goal met)" : "")}
            • New cards today: {daily.NewCards.Current} / {daily.NewCards.Target}{(daily.NewCards.IsCompleted ? " (goal met)" : "")}
            • Total terms: {vocab.TotalLemmas}
            • Known (mature): {vocab.MatureCount}
            • Learning: {vocab.LearningCount}
            • New: {vocab.NewCount}
            """);

        return new AgentExecutionResult(
            content,
            new AgentDomainDecision(true, AgentDomainCategory.Progress),
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "progress",
            Actions: [BuildNavigateAction(AgentNavigateDestination.Study, firstDeckId), BuildNavigateAction(AgentNavigateDestination.Vocabulary, firstDeckId)]);
    }

    private async Task<AgentExecutionResult> HandleExplainWordAsync(
        RoutedAgentIntent intent,
        Guid userId,
        string sourceLang,
        string targetLang,
        string? firstDeckId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var word = intent.Word?.Trim();
        if (string.IsNullOrEmpty(word))
        {
            return new AgentExecutionResult(
                "Tell me which exact word or phrase to explain, e.g. Explain the word \"slept\".",
                intent.Domain!,
                Array.Empty<AgentToolCallRecord>(),
                IsError: true);
        }

        var prompt = $"""
            Explain the exact word or phrase "{word}" for a language learner.
            Source language: {sourceLang}. Explain in {targetLang}.
            Context sentence: {intent.Sentence ?? "(none)"}
            Use the exact surface form only. Do not use lemma labels.
            Answer briefly in {targetLang}.
            """;

        var explanation = AgentIntentRouter.SanitizeLemmaLabels(await _llmProvider.CompleteAsync(prompt, cancellationToken));
        var draft = new Dictionary<string, string> { ["Word"] = word };
        if (!string.IsNullOrEmpty(intent.Sentence))
            draft["Expression"] = intent.Sentence;

        return new AgentExecutionResult(
            explanation,
            intent.Domain!,
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "language_learning",
            Actions:
            [
                BuildEditorDraftAction(draft, "Create card", $"Save \"{word}\" as a flashcard draft."),
                BuildNavigateAction(AgentNavigateDestination.Vocabulary, firstDeckId)
            ]);
    }

    private async Task<AgentExecutionResult> HandleGrammarHelpAsync(
        RoutedAgentIntent intent,
        Guid userId,
        string targetLang,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var word = intent.Word?.Trim();
        if (string.IsNullOrEmpty(word))
        {
            return new AgentExecutionResult(
                "Include the exact word or phrase for grammar help, e.g. Why is \"went\" used here?",
                intent.Domain!,
                Array.Empty<AgentToolCallRecord>(),
                IsError: true);
        }

        var response = await _vocabularyClient.ExplainGrammarAsync(
            userId,
            intent.Sentence ?? word,
            word,
            targetLang,
            roles,
            cancellationToken);

        return new AgentExecutionResult(
            AgentIntentRouter.SanitizeLemmaLabels(response.Explanation),
            intent.Domain!,
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "language_learning");
    }

    private async Task<AgentExecutionResult> HandleGenerateExampleAsync(
        RoutedAgentIntent intent,
        Guid userId,
        string sourceLang,
        string targetLang,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var word = intent.Word?.Trim();
        if (string.IsNullOrEmpty(word))
        {
            return new AgentExecutionResult(
                "Which exact word or phrase should I use in an example sentence? Try: Example for \"memory\".",
                intent.Domain!,
                Array.Empty<AgentToolCallRecord>(),
                IsError: true);
        }

        var response = await _vocabularyClient.GenerateContextAsync(userId, word, sourceLang, roles, cancellationToken);
        var suggestion = response.Suggestions.FirstOrDefault();
        if (suggestion is null)
            throw new InvalidOperationException("Could not generate an example sentence");

        var content = AgentIntentRouter.SanitizeLemmaLabels(
            $"Example for \"{word}\":\n{suggestion.Sentence}\n\nTranslation:\n{suggestion.Translation}");

        var draft = new Dictionary<string, string>
        {
            ["Word"] = word,
            ["Expression"] = suggestion.Sentence,
            ["Translation"] = suggestion.Translation
        };

        return new AgentExecutionResult(
            content,
            intent.Domain!,
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "language_learning",
            Actions: [BuildEditorDraftAction(draft, "Use in Editor", "Open the card editor with this draft.")]);
    }

    private async Task<AgentExecutionResult> HandleBuildCardDraftAsync(
        RoutedAgentIntent intent,
        Guid userId,
        string sourceLang,
        string targetLang,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var word = intent.Word?.Trim();
        if (string.IsNullOrEmpty(word))
        {
            return new AgentExecutionResult(
                "Which exact word or phrase should the card use? Try: Create a flashcard for \"memory\".",
                intent.Domain!,
                Array.Empty<AgentToolCallRecord>(),
                IsError: true);
        }

        var draft = new Dictionary<string, string> { ["Word"] = word };
        if (!string.IsNullOrEmpty(intent.Sentence))
            draft["Expression"] = intent.Sentence;

        try
        {
            var response = await _vocabularyClient.GenerateContextAsync(userId, word, sourceLang, roles, cancellationToken);
            var suggestion = response.Suggestions.FirstOrDefault();
            if (suggestion is not null)
            {
                draft.TryAdd("Expression", suggestion.Sentence);
                draft.TryAdd("Translation", suggestion.Translation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Optional example generation failed for card draft");
        }

        var lines = new List<string> { $"Draft ready for exact surface form \"{word}\":" };
        if (draft.TryGetValue("Translation", out var translation))
            lines.Add($"Translation: {translation}");
        if (draft.TryGetValue("Expression", out var expression))
            lines.Add($"Example:\n{expression}");

        return new AgentExecutionResult(
            AgentIntentRouter.SanitizeLemmaLabels(string.Join("\n\n", lines)),
            intent.Domain!,
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "language_learning",
            Actions: [BuildEditorDraftAction(draft, "Open draft in Editor", "Review and save the card when ready.")]);
    }

    private static AgentExecutionResult HandleOutOfScope(string userText, string sourceLang, RoutedAgentIntent intent)
    {
        var domain = intent.Domain ?? AgentDomainPolicy.Classify(userText);
        return new AgentExecutionResult(
            AgentDomainPolicy.BuildOutOfScopeRefusal(userText, sourceLang),
            domain,
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "out_of_scope",
            Refusal: true,
            SuggestedPrompts: AgentDomainPolicy.RefusalSuggestedPrompts.ToList());
    }

    private async Task<AgentExecutionResult> HandleGeneralAnswerAsync(
        string userText,
        string projectTitle,
        string sourceLang,
        string targetLang,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
            You are PolyGuide, a language-learning copilot ONLY for project "{projectTitle}" ({sourceLang} → {targetLang}).

            The learner asked: {userText}

            STRICT RULES:
            - You ONLY help with language learning: vocabulary, grammar, translation, pronunciation, reading, cards, study, and progress in Polyraspad.
            - If the request is NOT about language learning (code, programming, homework, business, general trivia), refuse briefly and redirect to language-learning help.
            - Do NOT write code, algorithms, or general-purpose answers.
            - Use exact surface forms for words/phrases; never label vocabulary with "Lemma:" or treat base forms as learning status.
            - Answer briefly in {targetLang}. Suggest Reader, Editor, Study, or Vocabulary when helpful.
            - No markdown.
            """;

        var trimmed = AgentIntentRouter.SanitizeLemmaLabels(await _llmProvider.CompleteAsync(prompt, cancellationToken));
        var looksLikeRefusal = System.Text.RegularExpressions.Regex.IsMatch(
            trimmed,
            @"\b(can't|cannot|can't help|i can only|i'm only|refuse|not able to write code|language learning)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return new AgentExecutionResult(
            trimmed,
            new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning),
            Array.Empty<AgentToolCallRecord>(),
            IntentCategory: "language_learning",
            Refusal: looksLikeRefusal);
    }

    private static AgentActionCard BuildNavigateAction(AgentNavigateDestination destination, string? firstDeckId)
    {
        var (title, href, label, kind) = destination switch
        {
            AgentNavigateDestination.Reader => ("Reader", "/reader", "Open Reader", "navigate"),
            AgentNavigateDestination.Editor => ("Create Card", "/editor", "Open Editor", "navigate"),
            AgentNavigateDestination.Study => ("Study", firstDeckId is not null ? $"/study/{firstDeckId}" : "/study", "Start Review", "start_study"),
            AgentNavigateDestination.Vocabulary => ("Vocabulary", "/vocabulary", "View Vocabulary", "navigate"),
            AgentNavigateDestination.Import => ("Import", "/import", "Open Import", "navigate"),
            _ => ("Library", "/library", "Open Library", "navigate")
        };

        return new AgentActionCard(
            $"nav-{destination.ToString().ToLowerInvariant()}",
            title,
            kind,
            href,
            label,
            $"Go to {title.ToLowerInvariant()}.");
    }

    private static AgentActionCard BuildEditorDraftAction(
        Dictionary<string, string> draft,
        string title,
        string description) =>
        new("open-editor-draft", title, "open_editor_draft", "/editor", "Open in Editor", description, draft);
}
