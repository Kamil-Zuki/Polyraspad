using System.Net;
using System.Net.Http.Json;
using AggregatorService.Dtos.Billing;
using AggregatorService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace AggregatorService.Tests;

public class BillingControllerTests : IClassFixture<AggregatorWebApplicationFactory>
{
    private readonly AggregatorWebApplicationFactory _factory;
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public BillingControllerTests(AggregatorWebApplicationFactory factory)
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
    public async Task GetAccess_ReturnsMappedAccessDto()
    {
        var mock = new Mock<IBillingServiceClient>();
        mock.Setup(x => x.CheckAccessAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessDto(true, "pro", "active", DateTime.UtcNow.AddDays(10)));

        _factory.BillingClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/Billing/access");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AccessDto>();
        dto.Should().NotBeNull();
        dto!.PlanCode.Should().Be("pro");
    }

    [Fact]
    public async Task ListPlans_ReturnsPlansFromBillingClient()
    {
        var mock = new Mock<IBillingServiceClient>();
        mock.Setup(x => x.ListPlansAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlanDto>
            {
                new("id1", "free", "Free", "desc", 0, "RUB", "month", true, true, 0,
                    new Dictionary<string, string> { ["maxProjects"] = "3" })
            });

        _factory.BillingClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/Billing/plans?onlyActive=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<List<PlanDto>>();
        plans.Should().NotBeNull();
        plans!.Count.Should().Be(1);
        plans[0].Code.Should().Be("free");
    }

    [Fact]
    public async Task ProcessWebhook_WithoutApiKeyConfigured_ReturnsOk()
    {
        var mock = new Mock<IBillingServiceClient>();
        mock.Setup(x => x.ProcessWebhookAsync("mock", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _factory.BillingClientMockHolder.Current = mock;

        using var client = _factory.CreateClient();
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Billing/webhooks/mock", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsage_ReturnsBillingUsageDto()
    {
        var mock = new Mock<IBillingServiceClient>();
        var usageDto = new BillingUsageDto(
            "free",
            new BillingUsageItemDto(2, 3, false),
            new BillingUsageItemDto(145, 500, false),
            new BillingUsageItemDto(4, 10, false),
            new BillingUsageItemDto(1, 3, false));

        mock.Setup(x => x.GetUsageAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageDto);

        _factory.BillingClientMockHolder.Current = mock;

        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/Billing/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BillingUsageDto>();
        dto.Should().NotBeNull();
        dto!.PlanCode.Should().Be("free");
        dto.Projects.Used.Should().Be(2);
        dto.Projects.Limit.Should().Be(3);
    }
}

