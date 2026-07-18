using AgentService.Dtos.Agent;
using AgentService.Orchestration;
using AgentService.Options;
using Microsoft.Extensions.Options;
using Pvs.Content.Grpc;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    private readonly IAgentThreadService _threadService;
    private readonly IVocabularyProjectAccessValidator _projectAccessValidator;
    private readonly IVocabularyGrpcClient _vocabularyClient;
    private readonly IAgentLlmProvider _llmProvider;
    private readonly AiOptions _aiOptions;
    private readonly ILogger<AgentOrchestrator> _logger;

    private static readonly AgentToolDefinition[] AvailableTools = new[]
    {
        new AgentToolDefinition(
            "create_deck",
            "Create a new deck for organizing vocabulary cards.",
            new {
                type = "object",
                properties = new {
                    title = new { type = "string", description = "Title of the deck" },
                    description = new { type = "string", description = "Optional description" }
                },
                required = new[] { "title" }
            }),
        new AgentToolDefinition(
            "create_card",
            "Create a new flashcard.",
            new {
                type = "object",
                properties = new {
                    deck_id = new { type = "string", description = "ID of the deck to add the card to. Ask the user if unknown." },
                    word = new { type = "string", description = "The exact word or phrase" },
                    translation = new { type = "string", description = "Translation in target language" },
                    expression = new { type = "string", description = "Optional example sentence using the word" }
                },
                required = new[] { "deck_id", "word", "translation" }
            }),
        new AgentToolDefinition(
            "get_user_vocabulary_stats",
            "Get the user's progress and vocabulary statistics.",
            new { type = "object", properties = new Dictionary<string, object>() }),
        new AgentToolDefinition(
            "get_recent_leeches",
            "Get a list of problematic (leech) cards the user struggles with.",
            new { type = "object", properties = new Dictionary<string, object>() }),
        new AgentToolDefinition(
            "mark_lesson_completed",
            "Mark the current lesson as completed. ONLY call this when the user has fully finished the lesson activities according to your assessment.",
            new {
                type = "object",
                properties = new {
                    lesson_id = new { type = "string", description = "ID of the lesson to mark as completed" }
                },
                required = new[] { "lesson_id" }
            }),
        new AgentToolDefinition(
            "submit_knowledge_check",
            "Submit the results of an exam or knowledge check to update the user's skill levels. Use this tool ONLY at the end of a Knowledge Check lesson.",
            new {
                type = "object",
                properties = new {
                    term_ids = new { type = "array", items = new { type = "string" }, description = "List of term IDs that were evaluated" },
                    reading_score = new { type = "integer", description = "Score for Reading (0-100), 0 if not evaluated" },
                    listening_score = new { type = "integer", description = "Score for Listening (0-100), 0 if not evaluated" },
                    writing_score = new { type = "integer", description = "Score for Writing (0-100), 0 if not evaluated" },
                    speaking_score = new { type = "integer", description = "Score for Speaking (0-100), 0 if not evaluated" }
                },
                required = new[] { "term_ids" }
            }),
        new AgentToolDefinition(
            "set_cefr_placement",
            "Set the user's CEFR level after a placement test. This unlocks curriculum lessons for them.",
            new {
                type = "object",
                properties = new {
                    cefr_level = new { type = "string", description = "The CEFR level determined by the test: A1, A2, B1, B2, C1, or C2" }
                },
                required = new[] { "cefr_level" }
            }),
        new AgentToolDefinition(
            "get_daily_plan",
            "Get the user's personalized daily learning plan: due flashcard count, weakest skill, next curriculum lesson, and skill CEFR levels. Call this at the start of any conversation if you need context about the user's current state.",
            new { type = "object", properties = new Dictionary<string, object>() }),
        new AgentToolDefinition(
            "generate_writing_task",
            "Get a list of words the user is currently learning to generate a writing or translation task for them.",
            new { type = "object", properties = new Dictionary<string, object>() }),
        new AgentToolDefinition(
            "get_skill_assessment_history",
            "Get the history of the user's skill assessments (reading, listening, writing, speaking scores) to analyze trends and suggest focused practice.",
            new { type = "object", properties = new Dictionary<string, object>() })
    };

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
        
        var history = await LoadHistoryAsync(userId, threadId, cancellationToken);
        var messages = new List<AgentChatMessageDto>(history)
        {
            new AgentChatMessageDto("user", request.UserText)
        };

        var thread = await _threadService.GetThreadAsync(userId, threadId, cancellationToken);

        var systemPrompt = !string.IsNullOrWhiteSpace(thread?.SystemPromptOverride) 
            ? thread.SystemPromptOverride 
            : AgentSystemPromptBuilder.Build(thread?.AgentId ?? "study-copilot", project.Title, sourceLang, targetLang);

        var intent = AgentIntentRouter.Route(request.UserText);
        if (intent.ToolId == AgentToolId.GeneratePractice)
        {
            var terms = await _vocabularyClient.GetLearningTermsAsync(userId, projectId, 5, roles, cancellationToken);
            if (terms.Any())
            {
                var termList = string.Join(", ", terms.Select(t => t.Text));
                systemPrompt += $"\n\n[SYSTEM INSTRUCTION]\nThe user wants to practice. Here are some words they are currently learning: {termList}. Generate a short creative writing exercise, translation task, or roleplay scenario where they must use these words. Do not give them the answers yet, encourage them to respond.";
            }
        }

        // If this is a greeting / init run, inject daily plan context into system prompt
        if (request.IsInitialGreeting && thread?.AgentId != "placement-copilot")
        {
            try
            {
                var plan = await _vocabularyClient.GetDailyPlanAsync(userId, projectId, roles, cancellationToken);
                var planSummary = BuildPlanSummary(plan);
                systemPrompt += $"\n\n[LEARNER CONTEXT — {DateTime.UtcNow:yyyy-MM-dd}]\n{planSummary}\n\nYour task: greet the learner warmly, summarize their plan in 2-3 sentences, and suggest the single most important thing to do right now based on the weakest skill. Be concise and motivating. Do NOT list all tasks in bullet points — just guide them naturally.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch daily plan for init greeting");
            }
        }

        var executedTools = new List<AgentToolCallRecord>();
        
        string assistantContent = string.Empty;
        int loops = 0;
        const int maxLoops = 5;
        
        while (loops < maxLoops)
        {
            loops++;
            var completion = await _llmProvider.CompleteChatAsync(systemPrompt, messages, AvailableTools, cancellationToken);
            
            if (completion.ToolCalls.Count == 0)
            {
                assistantContent = completion.Content;
                break;
            }
            
            // Append assistant's tool calls to context (so LLM knows what it asked to do)
            var contentToAppend = string.IsNullOrWhiteSpace(completion.Content) ? "Executing tool..." : completion.Content;
            messages.Add(new AgentChatMessageDto("assistant", contentToAppend));

            bool shouldBreak = false;
            foreach (var tc in completion.ToolCalls)
            {
                string outputJson;
                string status = "completed";
                
                try
                {
                    outputJson = await ExecuteToolCoreAsync(tc.Name, tc.Arguments, userId, projectId, roles, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tool execution failed: {Tool}", tc.Name);
                    outputJson = JsonSerializer.Serialize(new { error = ex.Message });
                    status = "failed";
                }
                
                executedTools.Add(new AgentToolCallRecord(tc.Name, tc.Arguments, outputJson, status));
                messages.Add(new AgentChatMessageDto("tool", outputJson));

                if (tc.Name == "set_cefr_placement" && status == "completed")
                {
                    shouldBreak = true;
                }
            }

            if (shouldBreak)
            {
                assistantContent = string.IsNullOrWhiteSpace(completion.Content) 
                    ? "Placement test completed. Your level has been updated." 
                    : completion.Content;
                break;
            }
        }
        var actions = new List<AgentActionCard>();
        var cleanLines = new List<string>();
        foreach (var line in assistantContent.Split('\n'))
        {
            var tLine = line.Trim();
            if (tLine.StartsWith("ACTION: ", StringComparison.OrdinalIgnoreCase))
            {
                tLine = tLine.Substring(8).Trim();
            }

            if (tLine.StartsWith("NAVIGATE|"))
            {
                var parts = tLine.Split('|');
                if (parts.Length >= 3)
                {
                    actions.Add(new AgentActionCard(
                        Guid.NewGuid().ToString(),
                        parts[2],
                        "navigate",
                        "/" + parts[1].TrimStart('/'),
                        "Open",
                        parts.Length >= 4 ? parts[3] : null));
                    continue;
                }
            }
            else if (tLine.StartsWith("OPEN_EDITOR_DRAFT|"))
            {
                var parts = tLine.Split('|');
                var draft = new Dictionary<string, string>();
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) draft["word"] = parts[1].Trim();
                if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])) draft["expression"] = parts[2].Trim();
                if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3])) draft["translation"] = parts[3].Trim();
                if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4])) draft["label"] = parts[4].Trim();
                if (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5])) draft["description"] = parts[5].Trim();

                actions.Add(new AgentActionCard(
                    Guid.NewGuid().ToString(),
                    "Draft Card",
                    "open_editor_draft",
                    "/editor",
                    "Open Editor",
                    "Draft a new card in the editor",
                    draft));
                continue;
            }
            cleanLines.Add(line);
        }
        assistantContent = string.Join("\n", cleanLines).Trim();

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

        var effectiveDomain = new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning);

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
            ToolCalls = executedTools.Select(toolCall => new AgentToolCallInputDto
            {
                ToolName = toolCall.ToolName,
                InputJson = toolCall.InputJson,
                OutputJson = toolCall.OutputJson,
                Status = toolCall.Status
            }).ToList(),
            Model = _aiOptions.Enabled ? _aiOptions.Model : null
        }, cancellationToken);
    }

    private async Task<string> ExecuteToolCoreAsync(
        string name, 
        string arguments, 
        Guid userId, 
        Guid projectId, 
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var args = JsonNode.Parse(arguments);
        
        switch (name)
        {
            case "create_deck":
                var title = args?["title"]?.GetValue<string>() ?? "New Deck";
                var desc = args?["description"]?.GetValue<string>();
                var deck = await _vocabularyClient.CreateDeckAsync(userId, projectId, title, desc, roles, cancellationToken);
                return JsonSerializer.Serialize(new { deck.Id, deck.Title });
                
            case "create_card":
                var deckIdStr = args?["deck_id"]?.GetValue<string>();
                if (!Guid.TryParse(deckIdStr, out var deckId) || deckId == Guid.Empty)
                {
                    var tree = await _vocabularyClient.GetDeckTreeAsync(userId, projectId, roles, cancellationToken);
                    var firstDeck = tree.RootDecks.FirstOrDefault();
                    if (firstDeck == null)
                        return JsonSerializer.Serialize(new { error = "No decks available in this project." });
                    deckId = Guid.Parse(firstDeck.Id);
                }
                    
                var word = args?["word"]?.GetValue<string>() ?? "";
                var translation = args?["translation"]?.GetValue<string>() ?? "";
                var expression = args?["expression"]?.GetValue<string>();
                var card = await _vocabularyClient.CreateCardAsync(userId, deckId, word, translation, expression, roles, cancellationToken);
                return JsonSerializer.Serialize(new { card.Id });
                
            case "get_user_vocabulary_stats":
                var vocab = await _vocabularyClient.GetVocabularyStatsAsync(userId, projectId, roles, cancellationToken);
                return JsonSerializer.Serialize(new { vocab.TotalLemmas, vocab.MatureCount, vocab.LearningCount, vocab.NewCount });
                
            case "get_recent_leeches":
                var leeches = await _vocabularyClient.GetLeechCardsAsync(userId, projectId, roles, cancellationToken);
                var mapped = leeches.Items.Select(c => new {
                    c.Id,
                    c.SrsStatus,
                    Word = c.Note?.FieldValues?.GetValueOrDefault("Word")?.StringValue ?? "Unknown",
                    Translation = c.Note?.FieldValues?.GetValueOrDefault("Translation")?.StringValue ?? "Unknown"
                });
                return JsonSerializer.Serialize(new { total = leeches.TotalCount, cards = mapped });
                
            case "mark_lesson_completed":
                var lessonIdStr = args?["lesson_id"]?.GetValue<string>();
                if (!Guid.TryParse(lessonIdStr, out var compLessonId))
                    return JsonSerializer.Serialize(new { error = "Invalid lesson_id format" });
                await _vocabularyClient.CompleteLessonAsync(userId, compLessonId, roles, cancellationToken);
                return JsonSerializer.Serialize(new { status = "success", message = "Lesson marked as completed successfully." });
                
            case "submit_knowledge_check":
                var termIdsNode = args?["term_ids"] as JsonArray;
                var termIds = termIdsNode?.Select(n => n?.GetValue<string>()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList() ?? new List<string>();
                var rScore = args?["reading_score"]?.GetValue<int>() ?? 0;
                var lScore = args?["listening_score"]?.GetValue<int>() ?? 0;
                var wScore = args?["writing_score"]?.GetValue<int>() ?? 0;
                var sScore = args?["speaking_score"]?.GetValue<int>() ?? 0;

                await _vocabularyClient.SubmitKnowledgeCheckResultAsync(userId, projectId, termIds!, rScore, lScore, wScore, sScore, roles, cancellationToken);
                return JsonSerializer.Serialize(new { status = "success", message = "Knowledge check results submitted successfully." });
                
            case "set_cefr_placement":
                var cefrLevel = args?["cefr_level"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(cefrLevel))
                    return JsonSerializer.Serialize(new { error = "cefr_level is required" });
                
                await _vocabularyClient.SetPlacementLevelAsync(userId, cefrLevel, roles, cancellationToken);
                return JsonSerializer.Serialize(new { status = "success", message = $"CEFR level set to {cefrLevel} successfully. All previous levels are unlocked." });

            case "get_daily_plan":
                var plan = await _vocabularyClient.GetDailyPlanAsync(userId, projectId, roles, cancellationToken);
                var summary = BuildPlanSummary(plan);
                return JsonSerializer.Serialize(new
                {
                    summary,
                    tasks = plan.Tasks.Select(t => new
                    {
                        t.TaskType,
                        t.Title,
                        t.Description,
                        t.DurationMinutes,
                        t.ActionUrl
                    })
                });

            case "generate_writing_task":
                var practiceTerms = await _vocabularyClient.GetLearningTermsAsync(userId, projectId, 7, roles, cancellationToken);
                return JsonSerializer.Serialize(new
                {
                    instruction = "Generate a short writing task (e.g. write a 3-sentence story, or translate a specific phrase) that requires the user to use the following words. Do not give them the answer. When they reply, evaluate their use of these words and their grammar, then call submit_knowledge_check to record their writing score (0-100) for these specific term_ids.",
                    terms = practiceTerms.Select(t => new { term_id = t.Id, text = t.Text })
                });

            case "get_skill_assessment_history":
                var history = await _vocabularyClient.GetSkillAssessmentHistoryAsync(userId, projectId, 20, roles, cancellationToken);
                return JsonSerializer.Serialize(new
                {
                    logs = history.Logs.Select(l => new
                    {
                        l.Skill,
                        l.Score,
                        Date = l.CreatedAt
                    })
                });

            default:
                throw new InvalidOperationException($"Unknown tool {name}");
        }
    }

    private static string BuildPlanSummary(GetDailyAutopilotPlanResponse plan)
    {
        var lines = new List<string>();
        var fsrsTask = plan.Tasks.FirstOrDefault(t => t.TaskType == "fsrs");
        var lessonTask = plan.Tasks.FirstOrDefault(t => t.TaskType == "lesson");
        var checkTask = plan.Tasks.FirstOrDefault(t => t.TaskType == "knowledge_check");

        if (fsrsTask != null)
            lines.Add($"Due flashcards: {fsrsTask.Title} ({fsrsTask.DurationMinutes} min)");
        if (lessonTask != null)
            lines.Add($"Next lesson: {lessonTask.Title} ({lessonTask.DurationMinutes} min)");
        if (checkTask != null)
            lines.Add($"Skill focus: {checkTask.Title} — {checkTask.Description}");

        return lines.Count > 0
            ? string.Join("\n", lines)
            : "No specific tasks for today. Encourage the learner to read or review vocabulary.";
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
