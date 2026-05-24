namespace AgentService.Data.Entities;

public class AgentArtifact
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public Guid ThreadId { get; set; }

    public string Kind { get; set; } = null!;

    public string PayloadJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    public virtual AgentRun Run { get; set; } = null!;

    public virtual AgentThread Thread { get; set; } = null!;
}
