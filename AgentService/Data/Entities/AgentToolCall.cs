namespace AgentService.Data.Entities;

public class AgentToolCall
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public string ToolName { get; set; } = null!;

    public string InputJson { get; set; } = "{}";

    public string OutputJson { get; set; } = "{}";

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AgentRun Run { get; set; } = null!;
}
