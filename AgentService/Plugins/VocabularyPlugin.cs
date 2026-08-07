using Microsoft.SemanticKernel;
using System.ComponentModel;
using Pvs.Content.Grpc;
using System.Text.Json;
using AgentService.Services;

namespace AgentService.Plugins;

public class VocabularyPlugin
{
    private readonly IVocabularyGrpcClient _vocabularyClient;
    private readonly Guid _userId;
    private readonly Guid _projectId;
    private readonly IEnumerable<string> _roles;

    public VocabularyPlugin(
        IVocabularyGrpcClient vocabularyClient, 
        Guid userId, 
        Guid projectId, 
        IEnumerable<string> roles)
    {
        _vocabularyClient = vocabularyClient;
        _userId = userId;
        _projectId = projectId;
        _roles = roles;
    }

    [KernelFunction, Description("Create a new deck for organizing vocabulary cards.")]
    public async Task<string> CreateDeckAsync(
        [Description("Title of the deck")] string title,
        [Description("Optional description")] string? description = null)
    {
        var deck = await _vocabularyClient.CreateDeckAsync(_userId, _projectId, title, description, _roles);
        return JsonSerializer.Serialize(new { deck.Id, deck.Title });
    }

    [KernelFunction, Description("Create a new flashcard.")]
    public async Task<string> CreateCardAsync(
        [Description("ID of the deck to add the card to. Ask the user if unknown.")] string deck_id,
        [Description("The exact word or phrase")] string word,
        [Description("Translation in target language")] string translation,
        [Description("Optional example sentence using the word")] string? expression = null)
    {
        if (!Guid.TryParse(deck_id, out var deckId) || deckId == Guid.Empty)
        {
            var tree = await _vocabularyClient.GetDeckTreeAsync(_userId, _projectId, _roles);
            var firstDeck = tree.RootDecks.FirstOrDefault();
            if (firstDeck == null)
                return JsonSerializer.Serialize(new { error = "No decks available in this project." });
            deckId = Guid.Parse(firstDeck.Id);
        }
            
        var card = await _vocabularyClient.CreateCardAsync(_userId, deckId, word, translation, expression, _roles);
        return JsonSerializer.Serialize(new { card.Id });
    }

    [KernelFunction, Description("Get the user's progress and vocabulary statistics.")]
    public async Task<string> GetUserVocabularyStatsAsync()
    {
        var vocab = await _vocabularyClient.GetVocabularyStatsAsync(_userId, _projectId, _roles);
        return JsonSerializer.Serialize(new { vocab.TotalLemmas, vocab.MatureCount, vocab.LearningCount, vocab.NewCount });
    }

    [KernelFunction, Description("Get a list of problematic (leech) cards the user struggles with.")]
    public async Task<string> GetRecentLeechesAsync()
    {
        var leeches = await _vocabularyClient.GetLeechCardsAsync(_userId, _projectId, _roles);
        var mapped = leeches.Items.Select(c => new {
            c.Id,
            c.SrsStatus,
            Word = c.Note?.FieldValues?.GetValueOrDefault("Word")?.StringValue ?? "Unknown",
            Translation = c.Note?.FieldValues?.GetValueOrDefault("Translation")?.StringValue ?? "Unknown"
        });
        return JsonSerializer.Serialize(new { total = leeches.TotalCount, cards = mapped });
    }

    [KernelFunction, Description("Mark the current lesson as completed. ONLY call this when the user has fully finished the lesson activities according to your assessment.")]
    public async Task<string> MarkLessonCompletedAsync(
        [Description("ID of the lesson to mark as completed")] string lesson_id)
    {
        if (!Guid.TryParse(lesson_id, out var compLessonId))
            return JsonSerializer.Serialize(new { error = "Invalid lesson_id format" });
        await _vocabularyClient.CompleteLessonAsync(_userId, compLessonId, _roles);
        return JsonSerializer.Serialize(new { status = "success", message = "Lesson marked as completed successfully." });
    }

    [KernelFunction, Description("Submit the results of an exam or knowledge check to update the user's skill levels. Use this tool ONLY at the end of a Knowledge Check lesson.")]
    public async Task<string> SubmitKnowledgeCheckAsync(
        [Description("List of term IDs that were evaluated")] List<string> term_ids,
        [Description("Score for Reading (0-100), 0 if not evaluated")] int reading_score = 0,
        [Description("Score for Listening (0-100), 0 if not evaluated")] int listening_score = 0,
        [Description("Score for Writing (0-100), 0 if not evaluated")] int writing_score = 0,
        [Description("Score for Speaking (0-100), 0 if not evaluated")] int speaking_score = 0)
    {
        await _vocabularyClient.SubmitKnowledgeCheckResultAsync(_userId, _projectId, term_ids, reading_score, listening_score, writing_score, speaking_score, _roles);
        return JsonSerializer.Serialize(new { status = "success", message = "Knowledge check results submitted successfully." });
    }

    [KernelFunction, Description("Set the user's CEFR level after a placement test. This unlocks curriculum lessons for them.")]
    public async Task<string> SetCefrPlacementAsync(
        [Description("The CEFR level determined by the test: A1, A2, B1, B2, C1, or C2")] string cefr_level)
    {
        if (string.IsNullOrWhiteSpace(cefr_level))
            return JsonSerializer.Serialize(new { error = "cefr_level is required" });
        
        await _vocabularyClient.SetPlacementLevelAsync(_userId, cefr_level, _roles);
        return JsonSerializer.Serialize(new { status = "success", message = $"CEFR level set to {cefr_level} successfully. All previous levels are unlocked." });
    }

    [KernelFunction, Description("Get the user's personalized daily learning plan: due flashcard count, weakest skill, next curriculum lesson, and skill CEFR levels. Call this at the start of any conversation if you need context about the user's current state.")]
    public async Task<string> GetDailyPlanAsync()
    {
        var plan = await _vocabularyClient.GetDailyPlanAsync(_userId, _projectId, _roles);
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
    }

    [KernelFunction, Description("Get a list of words the user is currently learning to generate a writing or translation task for them.")]
    public async Task<string> GenerateWritingTaskAsync()
    {
        var practiceTerms = await _vocabularyClient.GetLearningTermsAsync(_userId, _projectId, 7, _roles);
        return JsonSerializer.Serialize(new
        {
            instruction = "Generate a short writing task (e.g. write a 3-sentence story, or translate a specific phrase) that requires the user to use the following words. Do not give them the answer. When they reply, evaluate their use of these words and their grammar, then call submit_knowledge_check to record their writing score (0-100) for these specific term_ids.",
            terms = practiceTerms.Select(t => new { term_id = t.Id, text = t.Text })
        });
    }

    [KernelFunction, Description("Get the history of the user's skill assessments (reading, listening, writing, speaking scores) to analyze trends and suggest focused practice.")]
    public async Task<string> GetSkillAssessmentHistoryAsync()
    {
        var history = await _vocabularyClient.GetSkillAssessmentHistoryAsync(_userId, _projectId, 20, _roles);
        return JsonSerializer.Serialize(new
        {
            logs = history.Logs.Select(l => new
            {
                l.Skill,
                l.Score,
                Date = l.CreatedAt
            })
        });
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
}
