using BillingService.Data;
using BillingService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace BillingService.Tests.Helpers;

/// <summary>
/// InMemory-совместимый контекст без схемы billing и jsonb.
/// </summary>
public class TestBillingServiceContext : BillingServiceContext
{
    public TestBillingServiceContext(DbContextOptions<BillingServiceContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(null);

        modelBuilder.Entity<SaaSPlan>(entity =>
        {
            entity.Property(e => e.Entitlements)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                         ?? new Dictionary<string, string>());
        });
    }
}
