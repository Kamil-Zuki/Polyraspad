namespace AgentService.Dtos.Agent;

public class AgentThreadListItemDto
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Title { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }
}

public class AgentThreadDto
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Title { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public DateTime? ArchivedAt { get; init; }
}

public class AgentMessageDto
{
    public required Guid Id { get; init; }

    public required string Role { get; init; }

    public required string Content { get; init; }

    public string? MetadataJson { get; init; }

    public required DateTime CreatedAt { get; init; }
}

public class AgentMessageListDto
{
    public required IReadOnlyList<AgentMessageDto> Items { get; init; }

    public string? NextBefore { get; init; }
}

public class AgentMessageInputDto
{
    public Guid? Id { get; init; }

    public required string Role { get; init; }

    public required string Content { get; init; }

    public string? MetadataJson { get; init; }
}

public class AgentDomainDecisionInputDto
{
    public required bool Allowed { get; init; }

    public required string Category { get; init; }

    public string? Reason { get; init; }
}

public class AgentToolCallInputDto
{
    public required string ToolName { get; init; }

    public required string InputJson { get; init; }

    public required string OutputJson { get; init; }

    public required string Status { get; init; }
}

public class CreateAgentRunDto
{
    public required AgentMessageInputDto UserMessage { get; init; }

    public required AgentMessageInputDto AssistantMessage { get; init; }

    public required AgentDomainDecisionInputDto DomainDecision { get; init; }

    public required IReadOnlyList<AgentToolCallInputDto> ToolCalls { get; init; }

    public string? Model { get; init; }
}

public class ExecuteAgentRunDto
{
    public required string UserText { get; init; }

    public string? SourceLang { get; init; }

    public string? TargetLang { get; init; }

    public string? FirstDeckId { get; init; }
}

public class AgentRunDto
{
    public required Guid Id { get; init; }

    public required Guid ThreadId { get; init; }

    public required string Status { get; init; }

    public string? Model { get; init; }

    public required DateTime StartedAt { get; init; }

    public DateTime? CompletedAt { get; init; }
}

public class CreateAgentRunResultDto
{
    public required AgentRunDto Run { get; init; }

    public required AgentMessageDto UserMessage { get; init; }

    public required AgentMessageDto AssistantMessage { get; init; }
}

public class AgentArtifactDto
{
    public required Guid Id { get; init; }

    public required Guid RunId { get; init; }

    public required Guid ThreadId { get; init; }

    public required string Kind { get; init; }

    public required string PayloadJson { get; init; }

    public required DateTime CreatedAt { get; init; }
}

public class CreateAgentArtifactDto
{
    public required Guid RunId { get; init; }

    public required string Kind { get; init; }

    public required string PayloadJson { get; init; }
}
