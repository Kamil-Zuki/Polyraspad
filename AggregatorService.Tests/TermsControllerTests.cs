using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AggregatorService.Dtos;
using AggregatorService.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Moq;
using Pvs.Content.Grpc;
using Xunit;

namespace AggregatorService.Tests;

public class TermsControllerTests : IClassFixture<AggregatorWebApplicationFactory>
{
    private readonly AggregatorWebApplicationFactory _factory;
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string TestProjectId = "550e8400-e29b-41d4-a716-446655440000";

    public TermsControllerTests(AggregatorWebApplicationFactory factory)
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
    public async Task Get_terms_list_returns_200_and_items()
    {
        var updated = Timestamp.FromDateTime(DateTime.UtcNow);
        var grpc = new ListProjectTermsResponse();
        grpc.Items.Add(new ProjectTermListItem
        {
            TermId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            Text = "hello",
            NormalizedText = "hello",
            Type = "WORD",
            Language = "en",
            Status = "SAVED",
            Meaning = "привет",
            UpdatedAt = updated,
        });
        grpc.NextCursor = "next-cursor-token";

        var mock = new Mock<IVocabularyServiceClient>();
        ListProjectTermsRequest? captured = null;
        mock.Setup(x => x.ListProjectTermsAsync(It.IsAny<ListProjectTermsRequest>(), TestUserId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<ListProjectTermsRequest, Guid, IEnumerable<string>, CancellationToken>((req, _, _, _) => captured = req)
            .ReturnsAsync(grpc);

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync(
            $"/api/terms?projectId={TestProjectId}&status=SAVED&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.ProjectId.Should().Be(TestProjectId);
        captured.Status.Should().Be("SAVED");
        captured.PageSize.Should().Be(10);

        var body = await response.Content.ReadFromJsonAsync<ListProjectTermsResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(1);
        body.Items[0].Text.Should().Be("hello");
        body.Items[0].Status.Should().Be("SAVED");
        body.NextCursor.Should().Be("next-cursor-token");
    }

    [Fact]
    public async Task Purge_demo_import_returns_200_and_counts()
    {
        var mock = new Mock<IVocabularyServiceClient>();
        mock.Setup(x => x.PurgeDemoImportAsync(
                It.IsAny<PurgeDemoImportRequest>(),
                TestUserId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurgeDemoImportResponse
            {
                CardsDeleted = 3,
                StatusesDeleted = 2,
                TermsDeleted = 1,
            });

        _factory.VocabularyClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.PostAsync(
            $"/api/terms/purge-demo-import?projectId={TestProjectId}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PurgeDemoImportResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.Should().NotBeNull();
        body!.CardsDeleted.Should().Be(3);
        body.StatusesDeleted.Should().Be(2);
        body.TermsDeleted.Should().Be(1);
    }
}
