using System.Text.RegularExpressions;

namespace AgentService.Orchestration;

public enum AgentDomainCategory
{
    LanguageLearning,
    ProductNavigation,
    Progress,
    OutOfScope
}

public record AgentDomainDecision(bool Allowed, AgentDomainCategory Category, string? Reason = null)
{
    public string CategoryName => Category switch
    {
        AgentDomainCategory.LanguageLearning => "language_learning",
        AgentDomainCategory.ProductNavigation => "product_navigation",
        AgentDomainCategory.Progress => "progress",
        _ => "out_of_scope"
    };
}

public static class AgentDomainPolicy
{
    public static readonly string[] RefusalSuggestedPrompts =
    [
        "Translate this sentence",
        "Explain vocabulary from this text",
        "Create a flashcard for \"memory\""
    ];

    private static readonly Regex LearningMaterialOverride = new(
        @"\b(translate|vocabulary|words?|terms?|cards?|explain|meaning|grammar|learn)\b.*\b(from|in)\b.*\b(this|the)\b|\b(from|in)\b.*\b(this|the)\b.*\b(snippet|paragraph|text|code|error|message|comment|sentence)\b|\bwhat does\b.*\bmean\b.*\b(in|from)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HardOutOfScope = new(
        @"\b(write|implement|build|create|generate|make|code|program|debug|fix)\b.*\b(code|script|function|class|app|program|algorithm|api|backend|frontend)\b|\b(leetcode|homework solution|business plan|legal advice|medical advice)\b|\b(binary search|sort algorithm|machine learning model)\b|\bнапиши\s+код\b|\bнапиши\s+программ|\bреализуй\b.*\b(код|алгоритм|функци)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LanguageLearningSignals = new(
        @"\b(translate|translation|vocabulary|grammar|pronunciation|conjugat|tense|phrase|idiom|fluency|flashcard|sentence|word|phrase|language|english|russian|korean|german|french|spanish|japanese|chinese|learn|study|meaning|usage|difference between|how do (?:i|you) say|speak|read|write in)\b|\b(cefr|a1|a2|b1|b2|c1|c2)\b|\b(слово|фраза|перевед|граммат|произнош|изуч|язык|значени)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static AgentDomainDecision Classify(string userText)
    {
        var text = userText.Trim();
        if (string.IsNullOrEmpty(text))
            return new AgentDomainDecision(false, AgentDomainCategory.OutOfScope, "empty");

        if (LearningMaterialOverride.IsMatch(text))
            return new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning);

        if (HardOutOfScope.IsMatch(text))
            return new AgentDomainDecision(false, AgentDomainCategory.OutOfScope, "general_programming_or_non_learning_task");

        if (LanguageLearningSignals.IsMatch(text.ToLowerInvariant()))
            return new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning);

        return new AgentDomainDecision(false, AgentDomainCategory.OutOfScope, "not_language_learning");
    }

    public static string BuildOutOfScopeRefusal(string userText, string sourceLangLabel)
    {
        var mentionsCode = Regex.IsMatch(userText, @"c#|csharp|python|javascript|typescript|java", RegexOptions.IgnoreCase)
            || Regex.IsMatch(userText, @"\bcode\b", RegexOptions.IgnoreCase)
            || userText.Contains("код", StringComparison.OrdinalIgnoreCase)
            || HardOutOfScope.IsMatch(userText);

        if (mentionsCode)
        {
            return $"""
                I can't write or implement code here. PolyGuide is for language learning in {sourceLangLabel}.

                Try one of these instead:
                • Translate comments or error messages from the snippet
                • Explain vocabulary like "class", "method", or "Console.WriteLine"
                • Create flashcards from terms in the text
                """;
        }

        return """
            I can only help with language learning in Polyraspad — vocabulary, grammar, reading, cards, study, and progress.

            Try asking me to explain a word, translate a sentence, draft a card, or open Reader / Study.
            """;
    }
}
