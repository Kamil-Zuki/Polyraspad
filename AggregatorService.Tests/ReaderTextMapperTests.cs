using AggregatorService.Dtos.Reader;
using AggregatorService.Helpers;
using FluentAssertions;
using Pvs.Content.Grpc;
using Xunit;

namespace AggregatorService.Tests;

public class ReaderTextMapperTests
{
    [Fact]
    public void ToHttpResponse_MapsPhraseSpansAndProjectTermIds()
    {
        var grpc = new AnalyzeTextResponse
        {
            Tokens =
            {
                new TextToken { Text = "take", Type = TokenType.Word, Status = TokenStatus.New },
                new TextToken { Text = " ", Type = TokenType.Space },
                new TextToken { Text = "off", Type = TokenType.Word, Status = TokenStatus.New },
            },
            Phrases =
            {
                new TextPhrase
                {
                    StartIndex = 0,
                    EndIndex = 2,
                    Text = "take off",
                    Status = TokenStatus.Learning,
                    ProjectTermId = "pt-1"
                }
            },
            Stats = new TextAnalysisStats { UniqueWords = 2, KnownPercentage = 0.5 }
        };

        var dto = ReaderTextMapper.ToHttpResponse(grpc);

        dto.Phrases.Should().ContainSingle();
        dto.Phrases[0].Text.Should().Be("take off");
        dto.Phrases[0].ProjectTermId.Should().Be("pt-1");
        dto.Phrases[0].Status.Should().Be("LEARNING");
    }
}
