using System.Net;
using System.Net.Http.Json;
using AggregatorService.Dtos;
using AggregatorService.Dtos.Subscriptions;
using AggregatorService.Services;
using FluentAssertions;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace AggregatorService.Tests;

public class SubscriptionsControllerTests : IClassFixture<AggregatorWebApplicationFactory>
{
    private readonly AggregatorWebApplicationFactory _factory;
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestDeckId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public SubscriptionsControllerTests(AggregatorWebApplicationFactory factory)
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
    public async Task should_return_200_and_mapped_list_when_user_has_subscriptions()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var item = new SubscriptionListItemDto
        {
            DeckId = TestDeckId,
            ProjectId = Guid.NewGuid(),
            Title = "Test Deck",
            SubscribedAt = DateTime.UtcNow.AddDays(-1),
            LastAccessedAt = DateTime.UtcNow,
            LastSyncedVersion = 2
        };
        mock.Setup(x => x.ListSubscriptionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<DeckSubscriptionDto>>();
        list.Should().NotBeNull();
        list!.Count.Should().Be(1);
        list[0].Id.Should().Be(TestDeckId.ToString());
        list[0].UserId.Should().Be(TestUserId.ToString());
        list[0].DeckId.Should().Be(TestDeckId.ToString());
        list[0].LastSyncedVersion.Should().Be(2);
        list[0].DeckTitle.Should().Be("Test Deck");
    }

    [Fact]
    public async Task should_return_200_empty_array_when_user_has_no_subscriptions()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        mock.Setup(x => x.ListSubscriptionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubscriptionListItemDto>());

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<DeckSubscriptionDto>>();
        list.Should().NotBeNull();
        list!.Count.Should().Be(0);
    }

    [Fact]
    public async Task should_return_401_when_list_subscriptions_without_auth()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task should_return_201_and_mapped_dto_when_subscribe_to_deck()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var item = new SubscriptionListItemDto
        {
            DeckId = TestDeckId,
            ProjectId = Guid.NewGuid(),
            Title = "Subscribed Deck",
            SubscribedAt = DateTime.UtcNow,
            LastAccessedAt = null,
            LastSyncedVersion = 0
        };
        mock.Setup(x => x.SubscribeAsync(TestUserId, TestDeckId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/subscriptions/{TestDeckId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<DeckSubscriptionDto>();
        dto.Should().NotBeNull();
        dto!.DeckId.Should().Be(TestDeckId.ToString());
        dto.UserId.Should().Be(TestUserId.ToString());
        dto.DeckTitle.Should().Be("Subscribed Deck");
    }

    [Fact]
    public async Task should_return_204_when_unsubscribe_from_subscribed_deck()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        mock.Setup(x => x.UnsubscribeAsync(TestUserId, TestDeckId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/subscriptions/{TestDeckId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task should_return_204_when_unsubscribe_already_unsubscribed()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        mock.Setup(x => x.UnsubscribeAsync(TestUserId, TestDeckId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/subscriptions/{TestDeckId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task should_return_404_when_subscribe_to_nonexistent_deck()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        mock.Setup(x => x.SubscribeAsync(TestUserId, TestDeckId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RpcException(new Status(StatusCode.NotFound, "Deck not found")));

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/subscriptions/{TestDeckId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task should_return_403_when_subscribe_to_private_deck_without_access()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        mock.Setup(x => x.SubscribeAsync(TestUserId, TestDeckId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RpcException(new Status(StatusCode.PermissionDenied, "Access denied")));

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/subscriptions/{TestDeckId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
