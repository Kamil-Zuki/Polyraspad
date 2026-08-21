using BillingService.Data.Entities;
using BillingService.Services;
using BillingService.Tests.Helpers;
using FluentAssertions;

namespace BillingService.Tests;

public class AccessServiceTests
{
    [Fact]
    public async Task CheckAccess_ActiveSubscription_ReturnsPaidPlan()
    {
        await using var context = BillingTestDb.CreateContext();
        var userId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = "user@test.com",
            Provider = BillingProvider.Mock,
            CreatedAt = DateTime.UtcNow
        };
        var proPlan = context.Plans.First(p => p.Code == "pro");
        context.Customers.Add(customer);
        context.Subscriptions.Add(new BillingSubscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PlanId = proPlan.Id,
            Provider = BillingProvider.Mock,
            ManagementMode = SubscriptionManagementMode.LocallyManaged,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new AccessService(context, BillingTestDb.CreateBillingOptions(), BillingTestDb.CreateNullLogger<AccessService>());
        var result = await service.CheckAccessAsync(userId);

        result.HasAccess.Should().BeTrue();
        result.PlanCode.Should().Be("pro");
        result.Status.Should().Be("active");
    }

    [Fact]
    public async Task CheckAccess_NoSubscription_FallsBackToFreePlan()
    {
        await using var context = BillingTestDb.CreateContext();
        var userId = Guid.NewGuid();

        var service = new AccessService(context, BillingTestDb.CreateBillingOptions(), BillingTestDb.CreateNullLogger<AccessService>());
        var result = await service.CheckAccessAsync(userId);

        result.HasAccess.Should().BeTrue();
        result.PlanCode.Should().Be("free");
    }

    [Fact]
    public async Task CheckAccess_ExpiredPeriod_FallsBackToFreePlan()
    {
        await using var context = BillingTestDb.CreateContext();
        var userId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = "user@test.com",
            Provider = BillingProvider.Mock,
            CreatedAt = DateTime.UtcNow
        };
        var proPlan = context.Plans.First(p => p.Code == "pro");
        context.Customers.Add(customer);
        context.Subscriptions.Add(new BillingSubscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PlanId = proPlan.Id,
            Provider = BillingProvider.Mock,
            ManagementMode = SubscriptionManagementMode.LocallyManaged,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow.AddMonths(-2),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new AccessService(context, BillingTestDb.CreateBillingOptions(), BillingTestDb.CreateNullLogger<AccessService>());
        var result = await service.CheckAccessAsync(userId);

        result.PlanCode.Should().Be("free");
    }

    [Fact]
    public async Task CheckAccess_PastDueWithinGracePeriod_KeepsPaidPlan()
    {
        await using var context = BillingTestDb.CreateContext();
        var userId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = "user@test.com",
            Provider = BillingProvider.Mock,
            CreatedAt = DateTime.UtcNow
        };
        var proPlan = context.Plans.First(p => p.Code == "pro");
        context.Customers.Add(customer);
        context.Subscriptions.Add(new BillingSubscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PlanId = proPlan.Id,
            Provider = BillingProvider.Mock,
            ManagementMode = SubscriptionManagementMode.LocallyManaged,
            Status = SubscriptionStatus.PastDue,
            CurrentPeriodStart = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new AccessService(context, BillingTestDb.CreateBillingOptions(gracePeriodDays: 3), BillingTestDb.CreateNullLogger<AccessService>());
        var result = await service.CheckAccessAsync(userId);

        result.HasAccess.Should().BeTrue();
        result.PlanCode.Should().Be("pro");
        result.Status.Should().Be("pastdue");
    }
}
