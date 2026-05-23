using AggregatorService.Dtos;
using AggregatorService.Mappers;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Pvs.Content.Grpc;
using Xunit;

namespace AggregatorService.Tests;

public class AutoMappingProfileTests
{
    [Fact]
    public void ProjectStats_MapsTotalLemmasToTotalTerms()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<AutoMappingProfile>(),
            NullLoggerFactory.Instance);
        var mapper = config.CreateMapper();

        var grpc = new ProjectStats
        {
            TotalLemmas = 42,
            MatureLemmas = 17,
        };

        var dto = mapper.Map<ProjectStatsDto>(grpc);

        dto.TotalTerms.Should().Be(42);
        dto.KnownTerms.Should().Be(17);
    }
}
