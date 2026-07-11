namespace AgentService.Data.Entities;

public class AgentThread
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProjectId { get; set; }

    public string? Title { get; set; }

    public string? AgentId { get; set; }

    public string? SystemPromptOverride { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public virtual ICollection<AgentMessage> Messages { get; set; } = new List<AgentMessage>();

    public virtual ICollection<AgentRun> Runs { get; set; } = new List<AgentRun>();

    public virtual ICollection<AgentArtifact> Artifacts { get; set; } = new List<AgentArtifact>();
}
