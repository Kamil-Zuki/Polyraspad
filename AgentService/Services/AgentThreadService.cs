using Microsoft.EntityFrameworkCore;
using AgentService.Data;
using AgentService.Data.Entities;
using AgentService.Dtos.Agent;
using AgentService.Helpers;

namespace AgentService.Services;

public class AgentThreadService : IAgentThreadService
{
    private readonly AgentServiceContext _context;
    private readonly IVocabularyProjectAccessValidator _projectAccessValidator;
    private readonly ILogger<AgentThreadService> _logger;

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "user", "assistant", "system", "tool"
    };

    private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "language_learning",
        "product_navigation",
        "progress",
        "out_of_scope",
        "automation"
    };

    private static readonly HashSet<string> ValidToolCallStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "completed", "failed"
    };

    public AgentThreadService(
        AgentServiceContext context,
        IVocabularyProjectAccessValidator projectAccessValidator,
        ILogger<AgentThreadService> logger)
    {
        _context = context;
        _projectAccessValidator = projectAccessValidator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentThreadListItemDto>> ListThreadsAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        string? agentId = null,
        CancellationToken cancellationToken = default)
    {
        await _projectAccessValidator.EnsureProjectAccessAsync(userId, projectId, roles, cancellationToken);

        var query = _context.AgentThreads
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ProjectId == projectId && t.ArchivedAt == null);

        if (!string.IsNullOrWhiteSpace(agentId))
            query = query.Where(t => t.AgentId == agentId);

        return await query
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new AgentThreadListItemDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Title = t.Title ?? AgentThreadTitleHelper.DefaultTitle,
                AgentId = t.AgentId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentThreadDto> CreateThreadAsync(
        Guid userId,
        Guid projectId,
        IEnumerable<string> roles,
        string? agentId = null,
        string? systemPromptOverride = null,
        CancellationToken cancellationToken = default)
    {
        await _projectAccessValidator.EnsureProjectAccessAsync(userId, projectId, roles, cancellationToken);

        var now = DateTime.UtcNow;
        var thread = new AgentThread
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            AgentId = string.IsNullOrWhiteSpace(agentId) ? null : agentId.Trim(),
            SystemPromptOverride = string.IsNullOrWhiteSpace(systemPromptOverride) ? null : systemPromptOverride.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.AgentThreads.Add(thread);
        await _context.SaveChangesAsync(cancellationToken);
        return MapThread(thread);
    }

    public async Task<AgentThreadDto?> GetThreadAsync(
        Guid userId,
        Guid threadId,
        CancellationToken cancellationToken = default)
    {
        var thread = await _context.AgentThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == threadId && t.UserId == userId, cancellationToken);

        return thread is null ? null : MapThread(thread);
    }

    public async Task<AgentMessageListDto?> ListMessagesAsync(
        Guid userId,
        Guid threadId,
        int limit,
        Guid? beforeMessageId,
        CancellationToken cancellationToken = default)
    {
        if (!await ThreadOwnedByUserAsync(userId, threadId, cancellationToken))
            return null;

        limit = Math.Clamp(limit, 1, 100);

        DateTime? beforeCreatedAt = null;
        if (beforeMessageId.HasValue)
        {
            beforeCreatedAt = await _context.AgentMessages
                .AsNoTracking()
                .Where(m => m.Id == beforeMessageId.Value && m.ThreadId == threadId)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (beforeCreatedAt is null)
                return new AgentMessageListDto { Items = Array.Empty<AgentMessageDto>(), NextBefore = null };
        }

        var query = _context.AgentMessages.AsNoTracking().Where(m => m.ThreadId == threadId);
        if (beforeCreatedAt.HasValue)
            query = query.Where(m => m.CreatedAt < beforeCreatedAt.Value);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        string? nextBefore = null;
        if (messages.Count > limit)
        {
            nextBefore = messages[limit].Id.ToString();
            messages = messages.Take(limit).ToList();
        }

        messages.Reverse();

        return new AgentMessageListDto
        {
            Items = messages.Select(MapMessage).ToList(),
            NextBefore = nextBefore
        };
    }

    public async Task<CreateAgentRunResultDto?> CreateRunAsync(
        Guid userId,
        Guid threadId,
        Guid projectId,
        CreateAgentRunDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRunRequest(request);

        var thread = await _context.AgentThreads
            .FirstOrDefaultAsync(t => t.Id == threadId && t.UserId == userId, cancellationToken);

        if (thread is null || thread.ProjectId != projectId)
            return null;

        if (thread.ArchivedAt.HasValue)
            throw new InvalidOperationException("Cannot create run on archived thread");

        var strategy = _context.Database.CreateExecutionStrategy();
        CreateAgentRunResultDto? result = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(thread.Title))
                    thread.Title = AgentThreadTitleHelper.DeriveTitle(request.UserMessage.Content);

                thread.UpdatedAt = now;

                var userMessage = new AgentMessage
                {
                    Id = request.UserMessage.Id ?? Guid.NewGuid(),
                    ThreadId = threadId,
                    Role = request.UserMessage.Role.ToLowerInvariant(),
                    Content = request.UserMessage.Content,
                    MetadataJson = AgentThreadTitleHelper.NormalizeMetadataJson(request.UserMessage.MetadataJson),
                    CreatedAt = now
                };

                var assistantMessage = new AgentMessage
                {
                    Id = request.AssistantMessage.Id ?? Guid.NewGuid(),
                    ThreadId = threadId,
                    Role = request.AssistantMessage.Role.ToLowerInvariant(),
                    Content = request.AssistantMessage.Content,
                    MetadataJson = AgentThreadTitleHelper.NormalizeMetadataJson(request.AssistantMessage.MetadataJson),
                    CreatedAt = now.AddMilliseconds(1)
                };

                var run = new AgentRun
                {
                    Id = Guid.NewGuid(),
                    ThreadId = threadId,
                    Status = "completed",
                    Model = request.Model,
                    StartedAt = now,
                    CompletedAt = now
                };

                var domainDecision = new AgentDomainDecision
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    Allowed = request.DomainDecision.Allowed,
                    Category = request.DomainDecision.Category.ToLowerInvariant(),
                    Reason = request.DomainDecision.Reason,
                    UserTextPreview = AgentThreadTitleHelper.BuildUserTextPreview(request.UserMessage.Content),
                    CreatedAt = now
                };

                _context.AgentMessages.AddRange(userMessage, assistantMessage);
                _context.AgentRuns.Add(run);
                _context.AgentDomainDecisions.Add(domainDecision);

                foreach (var toolCall in request.ToolCalls)
                {
                    _context.AgentToolCalls.Add(new AgentToolCall
                    {
                        Id = Guid.NewGuid(),
                        RunId = run.Id,
                        ToolName = toolCall.ToolName,
                        InputJson = toolCall.InputJson,
                        OutputJson = toolCall.OutputJson,
                        Status = toolCall.Status.ToLowerInvariant(),
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                result = new CreateAgentRunResultDto
                {
                    Run = MapRun(run),
                    UserMessage = MapMessage(userMessage),
                    AssistantMessage = MapMessage(assistantMessage)
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return result;
    }

    public async Task<bool> ArchiveThreadAsync(
        Guid userId,
        Guid threadId,
        CancellationToken cancellationToken = default)
    {
        var thread = await _context.AgentThreads
            .FirstOrDefaultAsync(t => t.Id == threadId && t.UserId == userId, cancellationToken);

        if (thread is null)
            return false;

        if (thread.ArchivedAt.HasValue)
            return true;

        thread.ArchivedAt = DateTime.UtcNow;
        thread.UpdatedAt = thread.ArchivedAt.Value;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AgentArtifactDto?> CreateArtifactAsync(
        Guid userId,
        Guid threadId,
        CreateAgentArtifactDto request,
        CancellationToken cancellationToken = default)
    {
        var thread = await _context.AgentThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == threadId && t.UserId == userId, cancellationToken);
        if (thread is null)
            return null;

        var runExists = await _context.AgentRuns
            .AnyAsync(r => r.Id == request.RunId && r.ThreadId == threadId, cancellationToken);
        if (!runExists)
            return null;

        var artifact = new AgentArtifact
        {
            Id = Guid.NewGuid(),
            RunId = request.RunId,
            ThreadId = threadId,
            Kind = request.Kind,
            PayloadJson = request.PayloadJson,
            CreatedAt = DateTime.UtcNow
        };

        _context.AgentArtifacts.Add(artifact);
        await _context.SaveChangesAsync(cancellationToken);
        return MapArtifact(artifact);
    }

    public async Task<IReadOnlyList<AgentArtifactDto>> ListArtifactsAsync(
        Guid userId,
        Guid threadId,
        Guid? runId,
        CancellationToken cancellationToken = default)
    {
        if (!await ThreadOwnedByUserAsync(userId, threadId, cancellationToken))
            return Array.Empty<AgentArtifactDto>();

        var query = _context.AgentArtifacts.AsNoTracking().Where(a => a.ThreadId == threadId);
        if (runId.HasValue)
            query = query.Where(a => a.RunId == runId.Value);

        var items = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(MapArtifact).ToList();
    }

    private async Task<bool> ThreadOwnedByUserAsync(
        Guid userId,
        Guid threadId,
        CancellationToken cancellationToken) =>
        await _context.AgentThreads.AsNoTracking()
            .AnyAsync(t => t.Id == threadId && t.UserId == userId, cancellationToken);

    private static void ValidateCreateRunRequest(CreateAgentRunDto request)
    {
        if (!ValidRoles.Contains(request.UserMessage.Role))
            throw new ArgumentException($"Invalid user message role: {request.UserMessage.Role}");

        if (!ValidRoles.Contains(request.AssistantMessage.Role))
            throw new ArgumentException($"Invalid assistant message role: {request.AssistantMessage.Role}");

        if (string.IsNullOrWhiteSpace(request.UserMessage.Content))
            throw new ArgumentException("User message content is required");

        if (string.IsNullOrWhiteSpace(request.AssistantMessage.Content))
            throw new ArgumentException("Assistant message content is required");

        if (!ValidCategories.Contains(request.DomainDecision.Category))
            throw new ArgumentException($"Invalid domain decision category: {request.DomainDecision.Category}");

        foreach (var toolCall in request.ToolCalls)
        {
            if (string.IsNullOrWhiteSpace(toolCall.ToolName))
                throw new ArgumentException("Tool name is required");

            if (!ValidToolCallStatuses.Contains(toolCall.Status))
                throw new ArgumentException($"Invalid tool call status: {toolCall.Status}");
        }
    }

    private static AgentThreadDto MapThread(AgentThread thread) => new()
    {
        Id = thread.Id,
        ProjectId = thread.ProjectId,
        Title = thread.Title ?? AgentThreadTitleHelper.DefaultTitle,
        AgentId = thread.AgentId,
        SystemPromptOverride = thread.SystemPromptOverride,
        CreatedAt = thread.CreatedAt,
        UpdatedAt = thread.UpdatedAt,
        ArchivedAt = thread.ArchivedAt
    };

    private static AgentMessageDto MapMessage(AgentMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        MetadataJson = message.MetadataJson,
        CreatedAt = message.CreatedAt
    };

    private static AgentRunDto MapRun(AgentRun run) => new()
    {
        Id = run.Id,
        ThreadId = run.ThreadId,
        Status = run.Status,
        Model = run.Model,
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt
    };

    private static AgentArtifactDto MapArtifact(AgentArtifact artifact) => new()
    {
        Id = artifact.Id,
        RunId = artifact.RunId,
        ThreadId = artifact.ThreadId,
        Kind = artifact.Kind,
        PayloadJson = artifact.PayloadJson,
        CreatedAt = artifact.CreatedAt
    };
}
