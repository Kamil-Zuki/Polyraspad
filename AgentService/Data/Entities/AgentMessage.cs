namespace AgentService.Data.Entities;

public class AgentMessage
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AgentThread Thread { get; set; } = null!;
}
