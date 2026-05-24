using AgentService.Dtos.Agent;

namespace AgentService.Services;

public interface IAgentThreadService
{
    Task<IReadOnlyList<AgentThreadListItemDto>> ListThreadsAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<AgentThreadDto> CreateThreadAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task<AgentThreadDto?> GetThreadAsync(
        Guid userId,
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task<AgentMessageListDto?> ListMessagesAsync(
        Guid userId,
        Guid threadId,
        int limit,
        Guid? beforeMessageId,
        CancellationToken cancellationToken = default);

    Task<CreateAgentRunResultDto?> CreateRunAsync(
        Guid userId,
        Guid threadId,
        Guid projectId,
        CreateAgentRunDto request,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveThreadAsync(
        Guid userId,
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task<AgentArtifactDto?> CreateArtifactAsync(
        Guid userId,
        Guid threadId,
        CreateAgentArtifactDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentArtifactDto>> ListArtifactsAsync(
        Guid userId,
        Guid threadId,
        Guid? runId,
        CancellationToken cancellationToken = default);
}
