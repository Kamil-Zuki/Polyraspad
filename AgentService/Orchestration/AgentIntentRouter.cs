using System.Text.RegularExpressions;

namespace AgentService.Orchestration;

public enum AgentToolId
{
    ExplainWord,
    GrammarHelp,
    GenerateExample,
    BuildCardDraft,
    GetProgress,
    Navigate,
    GeneralAnswer,
    OutOfScope
}

public enum AgentNavigateDestination
{
    Reader,
    Editor,
    Study,
    Vocabulary,
    Import,
    Library,
    Shadowing,
    Decks
}

public record RoutedAgentIntent(
    AgentToolId ToolId,
    string? Word = null,
    string? Sentence = null,
    AgentNavigateDestination? Destination = null,
    AgentDomainDecision? Domain = null)
{
    public string ToolName => ToolId switch
    {
        AgentToolId.ExplainWord => "explain_word",
        AgentToolId.GrammarHelp => "grammar_help",
        AgentToolId.GenerateExample => "generate_example",
        AgentToolId.BuildCardDraft => "build_card_draft",
        AgentToolId.GetProgress => "get_progress",
        AgentToolId.Navigate => "navigate",
        AgentToolId.GeneralAnswer => "general_answer",
        _ => "out_of_scope"
    };
}

public static class AgentIntentRouter
{
    private static readonly Regex Quoted = new(@"[""'«]([^""'»]+)[""'»]", RegexOptions.Compiled);

    public static string? ExtractTargetTerm(string text)
    {
        foreach (Match match in Quoted.Matches(text))
        {
            var term = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(term))
                return term;
        }

        var forWord = Regex.Match(text, @"\b(?:word|phrase|term)\s+[""']?([A-Za-zÀ-ÿ][\w\s'-]{0,40})", RegexOptions.IgnoreCase);
        if (forWord.Success && !string.IsNullOrWhiteSpace(forWord.Groups[1].Value))
            return forWord.Groups[1].Value.Trim();

        var explainMatch = Regex.Match(text,
            @"\b(?:explain|define|meaning of|what does|what is)\s+(?:the\s+)?(?:word|phrase|term)?\s*[""']?([A-Za-zÀ-ÿ][\w'-]{0,40})",
            RegexOptions.IgnoreCase);
        if (explainMatch.Success && !string.IsNullOrWhiteSpace(explainMatch.Groups[1].Value))
            return explainMatch.Groups[1].Value.Trim();

        var cardMatch = Regex.Match(text,
            @"\b(?:card|flashcard)\s+(?:for|about)\s+[""']?([A-Za-zÀ-ÿ][\w\s'-]{0,40})",
            RegexOptions.IgnoreCase);
        if (cardMatch.Success && !string.IsNullOrWhiteSpace(cardMatch.Groups[1].Value))
            return cardMatch.Groups[1].Value.Trim();

        return null;
    }

    public static RoutedAgentIntent Route(string userText)
    {
        var text = userText.Trim();
        var lower = text.ToLowerInvariant();

        var nav = MatchNavigation(lower);
        if (nav.HasValue)
        {
            return new RoutedAgentIntent(
                AgentToolId.Navigate,
                Word: ExtractTargetTerm(text),
                Destination: nav,
                Domain: new AgentDomainDecision(true, AgentDomainCategory.ProductNavigation));
        }

        if (Regex.IsMatch(lower, @"\bhow am i\b|\bmy progress\b|\bthis week\b|\bstreak\b|\bstats\b|\bhow am i doing\b"))
        {
            return new RoutedAgentIntent(
                AgentToolId.GetProgress,
                Domain: new AgentDomainDecision(true, AgentDomainCategory.Progress));
        }

        if (Regex.IsMatch(lower, @"\bgrammar\b|\bwhy (?:is|does|was|did)\b|\bwhy .* used\b"))
        {
            return new RoutedAgentIntent(
                AgentToolId.GrammarHelp,
                Word: ExtractTargetTerm(text),
                Sentence: ExtractSentenceContext(text),
                Domain: new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning));
        }

        if (Regex.IsMatch(lower, @"\bexample\b|\bsample sentence\b|\buse (?:it|this) in a sentence\b"))
        {
            return new RoutedAgentIntent(
                AgentToolId.GenerateExample,
                Word: ExtractTargetTerm(text),
                Domain: new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning));
        }

        if (Regex.IsMatch(lower, @"\bcreate\b.*\bcard\b|\bbuild\b.*\bcard\b|\bflashcard\b|\bmake a card\b|\bcards from\b"))
        {
            return new RoutedAgentIntent(
                AgentToolId.BuildCardDraft,
                Word: ExtractTargetTerm(text),
                Sentence: ExtractSentenceContext(text),
                Domain: new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning));
        }

        if (Regex.IsMatch(lower, @"\bexplain\b|\bwhat does\b|\bmeaning of\b|\bdefine\b|\bwhat is\b.*\bword\b"))
        {
            return new RoutedAgentIntent(
                AgentToolId.ExplainWord,
                Word: ExtractTargetTerm(text),
                Sentence: ExtractSentenceContext(text),
                Domain: new AgentDomainDecision(true, AgentDomainCategory.LanguageLearning));
        }

        var domain = AgentDomainPolicy.Classify(text);
        if (!domain.Allowed)
            return new RoutedAgentIntent(AgentToolId.OutOfScope, Domain: domain);

        return new RoutedAgentIntent(AgentToolId.GeneralAnswer, Domain: domain);
    }

    private static AgentNavigateDestination? MatchNavigation(string lower)
    {
        if (Regex.IsMatch(lower, @"\b(open|go to|show|launch)\b.*\breader\b|\bread books\b"))
            return AgentNavigateDestination.Reader;
        if (Regex.IsMatch(lower, @"\b(open|go to|launch)\b.*\beditor\b|\bcreate card\b|\bmake a card\b"))
            return AgentNavigateDestination.Editor;
        if (Regex.IsMatch(lower, @"\b(open|go to)\b.*\b(decks|my decks)\b"))
            return AgentNavigateDestination.Decks;
        if (Regex.IsMatch(lower, @"\b(open|go to)\b.*\blibrary\b|\bbooks\b"))
            return AgentNavigateDestination.Library;
        if (Regex.IsMatch(lower, @"\b(open|go to|show)\b.*\bvocab|\bmy words\b|\bsaved words\b"))
            return AgentNavigateDestination.Vocabulary;
        if (Regex.IsMatch(lower, @"\b(open|go to)\b.*\bimport\b|\bimport\b"))
            return AgentNavigateDestination.Import;
        if (Regex.IsMatch(lower, @"\b(open|go to|show|launch)\b.*\bshadow|\bpractice pronunciation\b"))
            return AgentNavigateDestination.Shadowing;
        if (Regex.IsMatch(lower, @"\bstart review\b|\bstudy now\b|\breview session\b|\bstart studying\b|\bstart a review\b"))
            return AgentNavigateDestination.Study;
        return null;
    }

    private static string? ExtractSentenceContext(string text)
    {
        var inContext = Regex.Match(text, @"\bin context(?: of)?[:\s]+(.+)", RegexOptions.IgnoreCase);
        if (inContext.Success && !string.IsNullOrWhiteSpace(inContext.Groups[1].Value))
            return inContext.Groups[1].Value.Trim();

        var sentenceLabel = Regex.Match(text, @"\bsentence[:\s]+(.+)", RegexOptions.IgnoreCase);
        if (sentenceLabel.Success && !string.IsNullOrWhiteSpace(sentenceLabel.Groups[1].Value))
            return sentenceLabel.Groups[1].Value.Trim();

        var quoted = Quoted.Matches(text);
        if (quoted.Count > 0)
        {
            var longest = quoted.Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Split(' ').Length > 1)
                .OrderByDescending(s => s.Length)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(longest))
                return longest;
        }

        return null;
    }

    public static string SanitizeLemmaLabels(string text) =>
        Regex.Replace(
            Regex.Replace(text, @"^\s*lemma\s*[:：]\s*.+$", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline),
            @"\bLemma:\s*\S+",
            string.Empty,
            RegexOptions.IgnoreCase)
            .Replace("\n\n\n", "\n\n", StringComparison.Ordinal)
            .Trim();
}
