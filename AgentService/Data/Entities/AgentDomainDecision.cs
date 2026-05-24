namespace AgentService.Data.Entities;

public class AgentDomainDecision
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public bool Allowed { get; set; }

    public string Category { get; set; } = null!;

    public string? Reason { get; set; }

    public string? UserTextPreview { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AgentRun Run { get; set; } = null!;
}
