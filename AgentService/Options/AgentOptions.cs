namespace AgentService.Options;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public int TimeoutSeconds { get; set; } = 120;

    public bool Enabled { get; set; } = true;
}

public class VocabularyServiceOptions
{
    public const string SectionName = "Vocabulary";

    public string GrpcAddress { get; set; } = "http://localhost:5117";
}
