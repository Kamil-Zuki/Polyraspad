using BillingService.Services;
using BillingService.Tests.Helpers;
using FluentAssertions;

namespace BillingService.Tests;

public class EntitlementServiceTests
{
    [Fact]
    public async Task GetEntitlements_WithoutSubscription_ReturnsFreeLimits()
    {
        await using var context = BillingTestDb.CreateContext();
        var service = new EntitlementService(context, BillingTestDb.CreateBillingOptions());

        var result = await service.GetEntitlementsAsync(Guid.NewGuid());

        result.PlanCode.Should().Be("free");
        result.Entitlements["maxProjects"].Should().Be("3");
        result.Entitlements["maxCards"].Should().Be("500");
    }
}
