using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using AggregatorService.Dtos;
using AggregatorService.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Moq;
using Pvs.Content.Grpc;
using Xunit;

namespace AggregatorService.Tests;

/// <summary>
/// Регрессия SR-VOC-02: UpdateCard должен передавать в gRPC обычные строки (без артефактов StringValue).
/// </summary>
public class CardsControllerTests : IClassFixture<AggregatorWebApplicationFactory>
{
    private readonly AggregatorWebApplicationFactory _factory;
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string TestCardId = "33333333-3333-3333-3333-333333333333";

    public CardsControllerTests(AggregatorWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", TestUserId.ToString());
        return client;
    }

    [Fact]
    public async Task should_return_200_when_UpdateCard_and_pass_plain_strings_to_grpc()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var grpcCard = new CardResponse
        {
            Id = TestCardId,
            DeckId = Guid.NewGuid().ToString(),
            CreatorId = TestUserId.ToString(),
            SrsStatus = SrsStatus.New,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
        };

        UpdateCardRequest? captured = null;
        mock.Setup(x => x.UpdateCardAsync(It.IsAny<UpdateCardRequest>(), TestUserId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateCardRequest, Guid, IEnumerable<string>, CancellationToken>((req, _, _, _) => captured = req)
            .ReturnsAsync(grpcCard);

        _factory.VocabularyClientMockHolder.Current = mock;

        var dto = new UpdateCardDto
        {
            FieldValues = new Dictionary<string, NoteFieldValueDto>
            {
                ["Expression"] = new() { StringValue = "Hello world" },
                ["Word"] = new() { StringValue = "world" },
                ["Translation"] = new() { StringValue = "Привет" },
            },
        };

        using var client = CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync($"/api/Cards/{TestCardId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.FieldValues["Expression"].StringValue.Should().Be("Hello world");
        captured.FieldValues["Word"].StringValue.Should().Be("world");
        captured.FieldValues["Translation"].StringValue.Should().Be("Привет");
        captured.CardId.Should().Be(TestCardId);
    }
}
