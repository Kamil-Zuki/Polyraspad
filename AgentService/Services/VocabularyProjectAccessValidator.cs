using Grpc.Core;
using Pvs.Content.Grpc;
using static Pvs.Content.Grpc.ContentService;

namespace AgentService.Services;

public interface IVocabularyProjectAccessValidator
{
    Task<ProjectResponse> EnsureProjectAccessAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}

public class VocabularyProjectAccessValidator : IVocabularyProjectAccessValidator
{
    private readonly ContentServiceClient _contentClient;
    private readonly ILogger<VocabularyProjectAccessValidator> _logger;

    public VocabularyProjectAccessValidator(
        ContentServiceClient contentClient,
        ILogger<VocabularyProjectAccessValidator> logger)
    {
        _contentClient = contentClient;
        _logger = logger;
    }

    public async Task<ProjectResponse> EnsureProjectAccessAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new Metadata
            {
                { "user_id", userId.ToString() },
                { "roles", string.Join(",", roles) }
            };

            return await _contentClient.GetProjectDetailsAsync(
                new GetProjectDetailsRequest
                {
                    UserId = userId.ToString(),
                    ProjectId = projectId.ToString()
                },
                headers: metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound || ex.StatusCode == StatusCode.PermissionDenied)
        {
            throw new KeyNotFoundException($"Project {projectId} not found or access denied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate project access for user {UserId} project {ProjectId}", userId, projectId);
            throw;
        }
    }
}
