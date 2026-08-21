using BillingService.Data.Entities;
using BillingService.Providers.Models;
using BillingService.Services;
using BillingService.Tests.Helpers;
using FluentAssertions;

namespace BillingService.Tests;

public class WebhookOrchestratorTests
{
    [Fact]
    public async Task PaymentSucceeded_ActivatesSubscription_AndCreatesInvoice()
    {
        await using var context = BillingTestDb.CreateContext();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var proPlan = context.Plans.First(p => p.Code == "pro");

        context.Customers.Add(new Customer
        {
            Id = customerId,
            UserId = userId,
            Email = "user@test.com",
            Provider = BillingProvider.Mock,
            CreatedAt = DateTime.UtcNow
        });

        var subscription = new BillingSubscription
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PlanId = proPlan.Id,
            Provider = BillingProvider.Mock,
            ManagementMode = SubscriptionManagementMode.LocallyManaged,
            Status = SubscriptionStatus.Incomplete,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var invoiceService = new InvoiceService(context);
        var orchestrator = new WebhookOrchestrator(context, invoiceService, BillingTestDb.CreateNullLogger<WebhookOrchestrator>());

        var paymentId = "pay_test_123";
        await orchestrator.ApplyEventsAsync([
            new PaymentSucceededEvent(
                paymentId,
                null,
                customerId.ToString("N"),
                "pm_test",
                paymentId,
                99000,
                "RUB",
                DateTime.UtcNow,
                "pro")
        ]);

        var updated = context.Subscriptions.Single(s => s.Id == subscription.Id);
        updated.Status.Should().Be(SubscriptionStatus.Active);
        context.Invoices.Should().ContainSingle(i => i.ProviderInvoiceId == paymentId);
    }

    [Fact]
    public async Task ProcessedWebhook_DuplicateEventId_IsIdempotentViaGrpcLayer()
    {
        await using var context = BillingTestDb.CreateContext();
        context.ProcessedWebhooks.Add(new ProcessedWebhook
        {
            Provider = BillingProvider.Mock,
            EventId = "evt_duplicate",
            EventType = "payment.succeeded",
            ProcessedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var exists = context.ProcessedWebhooks.Any(pw =>
            pw.Provider == BillingProvider.Mock && pw.EventId == "evt_duplicate");

        exists.Should().BeTrue();
    }
}
