using FluentValidation;
using Pvs.Agent.Grpc;

namespace AgentService.Validations;

public class ListAgentThreadsRequestValidator : AbstractValidator<ListAgentThreadsRequest>
{
    public ListAgentThreadsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid).WithMessage("User ID must be a valid UUID");
        RuleFor(x => x.ProjectId).NotEmpty().Must(BeValidGuid).WithMessage("Project ID must be a valid UUID");
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class CreateAgentThreadRequestValidator : AbstractValidator<CreateAgentThreadRequest>
{
    public CreateAgentThreadRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid).WithMessage("User ID must be a valid UUID");
        RuleFor(x => x.ProjectId).NotEmpty().Must(BeValidGuid).WithMessage("Project ID must be a valid UUID");
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class GetAgentThreadRequestValidator : AbstractValidator<GetAgentThreadRequest>
{
    public GetAgentThreadRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid).WithMessage("User ID must be a valid UUID");
        RuleFor(x => x.ThreadId).NotEmpty().Must(BeValidGuid).WithMessage("Thread ID must be a valid UUID");
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class ListAgentMessagesRequestValidator : AbstractValidator<ListAgentMessagesRequest>
{
    public ListAgentMessagesRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid).WithMessage("User ID must be a valid UUID");
        RuleFor(x => x.ThreadId).NotEmpty().Must(BeValidGuid).WithMessage("Thread ID must be a valid UUID");
        RuleFor(x => x.Limit).InclusiveBetween(1, 100).When(x => x.Limit != 0);
        RuleFor(x => x.Before).Must(v => v == null || string.IsNullOrEmpty(v) || Guid.TryParse(v, out _))
            .When(x => x.Before != null && !string.IsNullOrEmpty(x.Before));
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class CreateAgentRunRequestValidator : AbstractValidator<CreateAgentRunRequest>
{
    public CreateAgentRunRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid).WithMessage("User ID must be a valid UUID");
        RuleFor(x => x.ThreadId).NotEmpty().Must(BeValidGuid).WithMessage("Thread ID must be a valid UUID");
        RuleFor(x => x.ProjectId).NotEmpty().Must(BeValidGuid).WithMessage("Project ID must be a valid UUID");
        RuleFor(x => x.UserMessage).NotNull();
        RuleFor(x => x.AssistantMessage).NotNull();
        RuleFor(x => x.DomainDecision).NotNull();
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class ExecuteAgentRunRequestValidator : AbstractValidator<ExecuteAgentRunRequest>
{
    public ExecuteAgentRunRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid).WithMessage("User ID must be a valid UUID");
        RuleFor(x => x.ThreadId).NotEmpty().Must(BeValidGuid).WithMessage("Thread ID must be a valid UUID");
        RuleFor(x => x.ProjectId).NotEmpty().Must(BeValidGuid).WithMessage("Project ID must be a valid UUID");
        RuleFor(x => x.UserText).NotEmpty();
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class ArchiveAgentThreadRequestValidator : AbstractValidator<ArchiveAgentThreadRequest>
{
    public ArchiveAgentThreadRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid).WithMessage("User ID must be a valid UUID");
        RuleFor(x => x.ThreadId).NotEmpty().Must(BeValidGuid).WithMessage("Thread ID must be a valid UUID");
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class CreateAgentArtifactRequestValidator : AbstractValidator<CreateAgentArtifactRequest>
{
    public CreateAgentArtifactRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid);
        RuleFor(x => x.ThreadId).NotEmpty().Must(BeValidGuid);
        RuleFor(x => x.RunId).NotEmpty().Must(BeValidGuid);
        RuleFor(x => x.Kind).NotEmpty();
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class ListAgentArtifactsRequestValidator : AbstractValidator<ListAgentArtifactsRequest>
{
    public ListAgentArtifactsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeValidGuid);
        RuleFor(x => x.ThreadId).NotEmpty().Must(BeValidGuid);
        RuleFor(x => x.RunId).Must(v => v == null || string.IsNullOrEmpty(v) || Guid.TryParse(v, out _))
            .When(x => x.RunId != null && !string.IsNullOrEmpty(x.RunId));
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}
