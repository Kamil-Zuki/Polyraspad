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

    [Fact]
    public void CefrLevel_MapsWordsToNextLevel()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<AutoMappingProfile>(),
            NullLoggerFactory.Instance);
        var mapper = config.CreateMapper();

        var grpc = new CefrLevel
        {
            Code = "A1",
            Title = "Beginner",
            ProgressPercent = 0,
            WordsToNextLevel = 497,
        };

        var dto = mapper.Map<CefrLevelDto>(grpc);

        dto.Code.Should().Be("A1");
        dto.WordsToNextLevel.Should().Be(497);
    }
}
