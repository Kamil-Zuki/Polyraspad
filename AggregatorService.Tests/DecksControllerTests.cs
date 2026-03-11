using System.Net;
using System.Net.Http.Json;
using AggregatorService.Dtos;
using AggregatorService.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Pvs.Content.Grpc;
using Xunit;

namespace AggregatorService.Tests;

public class DecksControllerTests : IClassFixture<AggregatorWebApplicationFactory>
{
    private readonly AggregatorWebApplicationFactory _factory;
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string TestDeckId = "22222222-2222-2222-2222-222222222222";

    public DecksControllerTests(AggregatorWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", TestUserId.ToString());
        return client;
    }

    /// <summary>
    /// Regression test: UpdateDeck with IsPublic=true/false must succeed (AutoMapper BoolValue->bool? fix).
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_return_200_when_UpdateDeck_with_IsPublic(bool isPublic)
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var deckResponse = new DeckResponse
        {
            Id = TestDeckId,
            ProjectId = Guid.NewGuid().ToString(),
            Title = "Updated Deck",
            IsPublic = isPublic,
            ContributionPolicy = ContributionPolicy.Closed,
            LicenseType = LicenseType.Private,
            CardCount = 0,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        UpdateDeckRequest? capturedRequest = null;
        mock.Setup(x => x.UpdateDeckAsync(It.IsAny<UpdateDeckRequest>(), TestUserId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateDeckRequest, Guid, IEnumerable<string>, CancellationToken>((req, _, _, _) => capturedRequest = req)
            .ReturnsAsync(deckResponse);

        _factory.VocabularyClientMockHolder.Current = mock;

        var dto = new UpdateDeckDto { IsPublic = isPublic };
        using var client = CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync($"/api/decks/{TestDeckId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.IsPublic.Should().Be(isPublic);
    }

    /// <summary>
    /// Regression test: UpdateDeck with IsPublic=null (omit) must succeed.
    /// </summary>
    [Fact]
    public async Task should_return_200_when_UpdateDeck_with_IsPublic_null()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var deckResponse = new DeckResponse
        {
            Id = TestDeckId,
            ProjectId = Guid.NewGuid().ToString(),
            Title = "Updated Deck",
            CardCount = 0,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        UpdateDeckRequest? capturedRequest = null;
        mock.Setup(x => x.UpdateDeckAsync(It.IsAny<UpdateDeckRequest>(), TestUserId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateDeckRequest, Guid, IEnumerable<string>, CancellationToken>((req, _, _, _) => capturedRequest = req)
            .ReturnsAsync(deckResponse);

        _factory.VocabularyClientMockHolder.Current = mock;

        var dto = new UpdateDeckDto { Title = "New Title" };
        using var client = CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync($"/api/decks/{TestDeckId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.IsPublic.Should().BeNull();
    }

    [Fact]
    public async Task should_return_401_when_UpdateDeck_without_auth()
    {
        using var client = _factory.CreateClient();
        var dto = new UpdateDeckDto { IsPublic = true };
        var response = await client.PutAsJsonAsync($"/api/decks/{TestDeckId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
