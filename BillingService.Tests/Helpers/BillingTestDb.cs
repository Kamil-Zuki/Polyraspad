using BillingService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using BillingService.Options;
using Microsoft.Extensions.Options;

namespace BillingService.Tests.Helpers;

public static class BillingTestDb
{
    public static BillingServiceContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingServiceContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new TestBillingServiceContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static IOptions<BillingOptions> CreateBillingOptions(int gracePeriodDays = 3) =>
        Microsoft.Extensions.Options.Options.Create(new BillingOptions { GracePeriodDays = gracePeriodDays });

    public static ILogger<T> CreateNullLogger<T>() =>
        Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
}
