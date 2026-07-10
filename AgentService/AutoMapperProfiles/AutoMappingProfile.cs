using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Pvs.Agent.Grpc;
using AgentService.Dtos.Agent;
using AgentService.Helpers;

namespace AgentService.AutoMapperProfiles;

public class AutoMappingProfile : Profile
{
    public AutoMappingProfile()
    {
        CreateMap<AgentThreadListItemDto, AgentThreadListItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId.ToString()))
            .ForMember(dest => dest.AgentId, opt => opt.MapFrom(src => src.AgentId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc))))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.UpdatedAt, DateTimeKind.Utc))));

        CreateMap<AgentThreadDto, AgentThreadResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.ProjectId.ToString()))
            .ForMember(dest => dest.AgentId, opt => opt.MapFrom(src => src.AgentId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc))))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.UpdatedAt, DateTimeKind.Utc))))
            .ForMember(dest => dest.ArchivedAt, opt => opt.MapFrom(src =>
                src.ArchivedAt.HasValue
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(src.ArchivedAt.Value, DateTimeKind.Utc))
                    : null));

        CreateMap<AgentMessageDto, AgentMessageItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.MetadataJson, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.MetadataJson) ? null : src.MetadataJson))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc))));

        CreateMap<AgentRunDto, AgentRunItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ThreadId, opt => opt.MapFrom(src => src.ThreadId.ToString()))
            .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.StartedAt, DateTimeKind.Utc))))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src =>
                src.CompletedAt.HasValue
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(src.CompletedAt.Value, DateTimeKind.Utc))
                    : null));

        CreateMap<AgentArtifactDto, AgentArtifactItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.RunId, opt => opt.MapFrom(src => src.RunId.ToString()))
            .ForMember(dest => dest.ThreadId, opt => opt.MapFrom(src => src.ThreadId.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc))));

        CreateMap<CreateAgentRunRequest, CreateAgentRunDto>()
            .ForMember(dest => dest.UserMessage, opt => opt.MapFrom(src => src.UserMessage))
            .ForMember(dest => dest.AssistantMessage, opt => opt.MapFrom(src => src.AssistantMessage))
            .ForMember(dest => dest.DomainDecision, opt => opt.MapFrom(src => src.DomainDecision))
            .ForMember(dest => dest.ToolCalls, opt => opt.MapFrom(src => src.ToolCalls))
            .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model != null && src.Model.Length > 0 ? src.Model : null));

        CreateMap<AgentMessageInput, AgentMessageInputDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => ParseOptionalGuid(src.Id)))
            .ForMember(dest => dest.MetadataJson, opt => opt.MapFrom(src =>
                src.MetadataJson != null && !string.IsNullOrEmpty(src.MetadataJson) ? src.MetadataJson : null));

        CreateMap<AgentDomainDecisionInput, AgentDomainDecisionInputDto>()
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src =>
                src.Reason != null && !string.IsNullOrEmpty(src.Reason) ? src.Reason : null));

        CreateMap<AgentToolCallInput, AgentToolCallInputDto>();
    }

    private static Guid? ParseOptionalGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var id) ? id : null;
    }
}
