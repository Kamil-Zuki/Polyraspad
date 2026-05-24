namespace AgentService.Data.Entities;

public class AgentRun
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public string Status { get; set; } = null!;

    public string? Model { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Error { get; set; }

    public virtual AgentThread Thread { get; set; } = null!;

    public virtual ICollection<AgentToolCall> ToolCalls { get; set; } = new List<AgentToolCall>();

    public virtual AgentDomainDecision? DomainDecision { get; set; }

    public virtual ICollection<AgentArtifact> Artifacts { get; set; } = new List<AgentArtifact>();
}
