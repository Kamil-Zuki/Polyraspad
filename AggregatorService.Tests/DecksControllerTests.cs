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
    /// When advanced modules are off, IsPublic=true is clamped to false (MVP defense in depth).
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_clamp_IsPublic_when_UpdateDeck_and_advanced_modules_disabled(bool isPublic)
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var deckResponse = new DeckResponse
        {
            Id = TestDeckId,
            ProjectId = Guid.NewGuid().ToString(),
            Title = "Updated Deck",
            IsPublic = false,
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

        var dto = new UpdateDeckDto
        {
            IsPublic = isPublic,
            ContributionPolicy = ContributionPolicyDto.Open,
        };
        using var client = CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync($"/api/decks/{TestDeckId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.IsPublic.Should().Be(false);
        capturedRequest.HasContributionPolicy.Should().BeFalse();
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
        // Title должен передаваться как обычная строка (без JSON-кавычек от StringValue.ToString())
        capturedRequest.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task should_return_401_when_UpdateDeck_without_auth()
    {
        using var client = _factory.CreateClient();
        var dto = new UpdateDeckDto { IsPublic = true };
        var response = await client.PutAsJsonAsync($"/api/decks/{TestDeckId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task should_clamp_IsPublic_when_CreateDeck_and_advanced_modules_disabled()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var projectId = Guid.NewGuid().ToString();
        var deckResponse = new DeckResponse
        {
            Id = TestDeckId,
            ProjectId = projectId,
            Title = "New Deck",
            IsPublic = false,
            ContributionPolicy = ContributionPolicy.Closed,
            LicenseType = LicenseType.Private,
            CardCount = 0,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        CreateDeckRequest? capturedRequest = null;
        mock.Setup(x => x.CreateDeckAsync(It.IsAny<CreateDeckRequest>(), TestUserId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<CreateDeckRequest, Guid, IEnumerable<string>, CancellationToken>((req, _, _, _) => capturedRequest = req)
            .ReturnsAsync(deckResponse);

        _factory.VocabularyClientMockHolder.Current = mock;

        var dto = new CreateDeckDto
        {
            ProjectId = projectId,
            Title = "New Deck",
            IsPublic = true,
        };
        using var client = CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/decks", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.IsPublic.Should().BeFalse();
    }
}

public class DecksControllerAdvancedModulesTests : IClassFixture<DecksControllerAdvancedModulesTests.AdvancedModulesEnabledFactory>
{
    private readonly AdvancedModulesEnabledFactory _factory;
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string TestDeckId = "22222222-2222-2222-2222-222222222222";

    public DecksControllerAdvancedModulesTests(AdvancedModulesEnabledFactory factory)
    {
        _factory = factory;
    }

    public sealed class AdvancedModulesEnabledFactory : AggregatorWebApplicationFactory
    {
        public AdvancedModulesEnabledFactory()
        {
            EnableAdvancedModulesForTests = true;
        }
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", TestUserId.ToString());
        return client;
    }

    /// <summary>
    /// Regression: with advanced modules on, IsPublic passes through (AutoMapper BoolValue->bool? fix).
    /// </summary>
    [Fact]
    public async Task should_pass_IsPublic_when_UpdateDeck_and_advanced_modules_enabled()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        var deckResponse = new DeckResponse
        {
            Id = TestDeckId,
            ProjectId = Guid.NewGuid().ToString(),
            Title = "Updated Deck",
            IsPublic = true,
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

        var dto = new UpdateDeckDto { IsPublic = true };
        using var client = CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync($"/api/decks/{TestDeckId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.IsPublic.Should().Be(true);
    }
}
