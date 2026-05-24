using AutoMapper;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Pvs.Agent.Grpc;
using AgentService.Dtos.Agent;
using AgentService.Helpers;
using AgentService.Services;
using static Pvs.Agent.Grpc.AgentService;

namespace AgentService.Grpc;

public class AgentGrpcService : AgentServiceBase
{
    private readonly ILogger<AgentGrpcService> _logger;
    private readonly IAgentThreadService _threadService;
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;
    private readonly IValidator<ListAgentThreadsRequest> _listThreadsValidator;
    private readonly IValidator<CreateAgentThreadRequest> _createThreadValidator;
    private readonly IValidator<GetAgentThreadRequest> _getThreadValidator;
    private readonly IValidator<ListAgentMessagesRequest> _listMessagesValidator;
    private readonly IValidator<CreateAgentRunRequest> _createRunValidator;
    private readonly IValidator<ExecuteAgentRunRequest> _executeRunValidator;
    private readonly IValidator<ArchiveAgentThreadRequest> _archiveThreadValidator;
    private readonly IValidator<CreateAgentArtifactRequest> _createArtifactValidator;
    private readonly IValidator<ListAgentArtifactsRequest> _listArtifactsValidator;

    public AgentGrpcService(
        ILogger<AgentGrpcService> logger,
        IAgentThreadService threadService,
        IAgentOrchestrator orchestrator,
        IMapper mapper,
        IHostEnvironment env,
        IValidator<ListAgentThreadsRequest> listThreadsValidator,
        IValidator<CreateAgentThreadRequest> createThreadValidator,
        IValidator<GetAgentThreadRequest> getThreadValidator,
        IValidator<ListAgentMessagesRequest> listMessagesValidator,
        IValidator<CreateAgentRunRequest> createRunValidator,
        IValidator<ExecuteAgentRunRequest> executeRunValidator,
        IValidator<ArchiveAgentThreadRequest> archiveThreadValidator,
        IValidator<CreateAgentArtifactRequest> createArtifactValidator,
        IValidator<ListAgentArtifactsRequest> listArtifactsValidator)
    {
        _logger = logger;
        _threadService = threadService;
        _orchestrator = orchestrator;
        _mapper = mapper;
        _env = env;
        _listThreadsValidator = listThreadsValidator;
        _createThreadValidator = createThreadValidator;
        _getThreadValidator = getThreadValidator;
        _listMessagesValidator = listMessagesValidator;
        _createRunValidator = createRunValidator;
        _executeRunValidator = executeRunValidator;
        _archiveThreadValidator = archiveThreadValidator;
        _createArtifactValidator = createArtifactValidator;
        _listArtifactsValidator = listArtifactsValidator;
    }

    public override async Task<ListAgentThreadsResponse> ListThreads(
        ListAgentThreadsRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_listThreadsValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));

        try
        {
            var roles = GrpcContextHelper.GetRoles(context);
            var threads = await _threadService.ListThreadsAsync(userId, projectId, roles, context.CancellationToken);
            var response = new ListAgentThreadsResponse();
            response.Items.AddRange(_mapper.Map<IEnumerable<AgentThreadListItem>>(threads));
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing agent threads");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<AgentThreadResponse> CreateThread(
        CreateAgentThreadRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_createThreadValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));

        try
        {
            var roles = GrpcContextHelper.GetRoles(context);
            var thread = await _threadService.CreateThreadAsync(userId, projectId, roles, context.CancellationToken);
            return _mapper.Map<AgentThreadResponse>(thread);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent thread");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<AgentThreadResponse> GetThread(
        GetAgentThreadRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_getThreadValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ThreadId, out var threadId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Thread ID format"));

        try
        {
            var thread = await _threadService.GetThreadAsync(userId, threadId, context.CancellationToken);
            if (thread is null)
                throw new RpcException(new Status(StatusCode.NotFound, "Thread not found"));

            return _mapper.Map<AgentThreadResponse>(thread);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent thread");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<ListAgentMessagesResponse> ListMessages(
        ListAgentMessagesRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_listMessagesValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ThreadId, out var threadId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Thread ID format"));

        Guid? before = null;
        if (request.Before != null && !string.IsNullOrEmpty(request.Before) && Guid.TryParse(request.Before, out var beforeId))
            before = beforeId;

        var limit = request.Limit <= 0 ? 100 : request.Limit;

        try
        {
            var messages = await _threadService.ListMessagesAsync(userId, threadId, limit, before, context.CancellationToken);
            if (messages is null)
                throw new RpcException(new Status(StatusCode.NotFound, "Thread not found"));

            var response = new ListAgentMessagesResponse();
            response.Items.AddRange(_mapper.Map<IEnumerable<AgentMessageItem>>(messages.Items));
            if (!string.IsNullOrEmpty(messages.NextBefore))
                response.NextBefore = messages.NextBefore;
            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing agent messages");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<CreateAgentRunResponse> CreateRun(
        CreateAgentRunRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_createRunValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ThreadId, out var threadId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Thread ID format"));

        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));

        try
        {
            var dto = _mapper.Map<CreateAgentRunDto>(request);
            var result = await _threadService.CreateRunAsync(userId, threadId, projectId, dto, context.CancellationToken);
            if (result is null)
                throw new RpcException(new Status(StatusCode.NotFound, "Thread not found"));

            return MapRunResponse(result);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent run");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<CreateAgentRunResponse> ExecuteRun(
        ExecuteAgentRunRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_executeRunValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ThreadId, out var threadId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Thread ID format"));

        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));

        try
        {
            var roles = GrpcContextHelper.GetRoles(context);
            var result = await _orchestrator.ExecuteRunAsync(
                userId,
                threadId,
                projectId,
                new ExecuteAgentRunDto
                {
                    UserText = request.UserText,
                    SourceLang = request.SourceLang,
                    TargetLang = request.TargetLang,
                    FirstDeckId = request.FirstDeckId
                },
                roles,
                context.CancellationToken);

            if (result is null)
                throw new RpcException(new Status(StatusCode.NotFound, "Thread not found"));

            return MapRunResponse(result);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent run");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<Empty> ArchiveThread(
        ArchiveAgentThreadRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_archiveThreadValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ThreadId, out var threadId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Thread ID format"));

        try
        {
            var archived = await _threadService.ArchiveThreadAsync(userId, threadId, context.CancellationToken);
            if (!archived)
                throw new RpcException(new Status(StatusCode.NotFound, "Thread not found"));

            return new Empty();
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving agent thread");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<AgentArtifactItem> CreateArtifact(
        CreateAgentArtifactRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_createArtifactValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ThreadId, out var threadId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Thread ID format"));

        if (!Guid.TryParse(request.RunId, out var runId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Run ID format"));

        try
        {
            var artifact = await _threadService.CreateArtifactAsync(
                userId,
                threadId,
                new CreateAgentArtifactDto
                {
                    RunId = runId,
                    Kind = request.Kind,
                    PayloadJson = request.PayloadJson
                },
                context.CancellationToken);

            if (artifact is null)
                throw new RpcException(new Status(StatusCode.NotFound, "Thread or run not found"));

            return _mapper.Map<AgentArtifactItem>(artifact);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent artifact");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    public override async Task<ListAgentArtifactsResponse> ListArtifacts(
        ListAgentArtifactsRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        await ValidateAsync(_listArtifactsValidator, request, context.CancellationToken);

        if (!Guid.TryParse(request.ThreadId, out var threadId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Thread ID format"));

        Guid? runId = null;
        if (request.RunId != null && !string.IsNullOrEmpty(request.RunId) && Guid.TryParse(request.RunId, out var parsedRunId))
            runId = parsedRunId;

        try
        {
            var artifacts = await _threadService.ListArtifactsAsync(userId, threadId, runId, context.CancellationToken);
            var response = new ListAgentArtifactsResponse();
            response.Items.AddRange(_mapper.Map<IEnumerable<AgentArtifactItem>>(artifacts));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing agent artifacts");
            throw new RpcException(new Status(StatusCode.Internal, _env.IsDevelopment() ? ex.Message : "Internal server error"));
        }
    }

    private CreateAgentRunResponse MapRunResponse(CreateAgentRunResultDto result) => new()
    {
        Run = _mapper.Map<AgentRunItem>(result.Run),
        UserMessage = _mapper.Map<AgentMessageItem>(result.UserMessage),
        AssistantMessage = _mapper.Map<AgentMessageItem>(result.AssistantMessage)
    };

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new RpcException(new Status(StatusCode.InvalidArgument, result.ToString()));
    }
}
