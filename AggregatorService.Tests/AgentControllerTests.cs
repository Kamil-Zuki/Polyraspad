using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AggregatorService.Dtos.Agent;
using AggregatorService.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Moq;
using Pvs.Agent.Grpc;
using Xunit;

namespace AggregatorService.Tests;

public class AgentControllerTests : IClassFixture<AggregatorWebApplicationFactory>
{
    private readonly AggregatorWebApplicationFactory _factory;
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string TestProjectId = "550e8400-e29b-41d4-a716-446655440000";
    private static readonly string TestThreadId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    public AgentControllerTests(AggregatorWebApplicationFactory factory)
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
    public async Task Get_threads_returns_200_and_items()
    {
        var updated = Timestamp.FromDateTime(DateTime.UtcNow);
        var grpc = new ListAgentThreadsResponse();
        grpc.Items.Add(new AgentThreadListItem
        {
            Id = TestThreadId,
            ProjectId = TestProjectId,
            Title = "How do I say hello?",
            CreatedAt = updated,
            UpdatedAt = updated
        });

        var mock = new Mock<IAgentServiceClient>();
        mock.Setup(x => x.ListAgentThreadsAsync(
                It.IsAny<ListAgentThreadsRequest>(),
                TestUserId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grpc);

        _factory.AgentClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/agent/threads?projectId={TestProjectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<AgentThreadListItemDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.Should().NotBeNull();
        body!.Should().HaveCount(1);
        body[0].Title.Should().Be("How do I say hello?");
    }

    [Fact]
    public async Task Post_runs_returns_200_with_messages()
    {
        var now = Timestamp.FromDateTime(DateTime.UtcNow);
        var mock = new Mock<IAgentServiceClient>();
        mock.Setup(x => x.ExecuteAgentRunAsync(
                It.IsAny<ExecuteAgentRunRequest>(),
                TestUserId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateAgentRunResponse
            {
                Run = new AgentRunItem
                {
                    Id = "run-id",
                    ThreadId = TestThreadId,
                    Status = "completed",
                    StartedAt = now,
                    CompletedAt = now
                },
                UserMessage = new AgentMessageItem
                {
                    Id = "user-msg",
                    Role = "user",
                    Content = "hello",
                    CreatedAt = now
                },
                AssistantMessage = new AgentMessageItem
                {
                    Id = "assistant-msg",
                    Role = "assistant",
                    Content = "Hola",
                    CreatedAt = now
                }
            });

        _factory.AgentClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync($"/api/agent/threads/{TestThreadId}/runs", new ExecuteAgentRunRequestDto
        {
            ProjectId = TestProjectId,
            UserText = "hello"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateAgentRunResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.Should().NotBeNull();
        body!.AssistantMessage.Content.Should().Be("Hola");
    }

    [Fact]
    public async Task Post_archive_returns_204()
    {
        var mock = new Mock<IAgentServiceClient>();
        mock.Setup(x => x.ArchiveAgentThreadAsync(
                It.IsAny<ArchiveAgentThreadRequest>(),
                TestUserId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _factory.AgentClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/agent/threads/{TestThreadId}/archive", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Get_thread_maps_grpc_not_found_to_404()
    {
        var mock = new Mock<IAgentServiceClient>();
        mock.Setup(x => x.GetAgentThreadAsync(
                It.IsAny<GetAgentThreadRequest>(),
                TestUserId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.NotFound, "Thread not found")));

        _factory.AgentClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/agent/threads/{TestThreadId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
