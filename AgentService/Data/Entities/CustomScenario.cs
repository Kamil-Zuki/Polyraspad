namespace AgentService.Data.Entities;

public class CustomScenario
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string TargetSkill { get; set; } = "Speaking";

    public string SystemPromptTemplate { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public List<string> Goals { get; set; } = new();

    public string? ContextConfiguration { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AgentThread> Threads { get; set; } = new List<AgentThread>();
}
