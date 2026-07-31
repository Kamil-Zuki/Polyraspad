using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class BillingLimitServiceTests
{
    private readonly DbContextOptions<VocabularyServiceContext> _dbOptions;

    public BillingLimitServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetMaxCardsAsync_WithFallback_Returns500()
    {
        // Arrange
        var billingClientMock = new Mock<IBillingEntitlementClient>();
        var redisMock = new Mock<IConnectionMultiplexer>();
        var loggerMock = new Mock<ILogger<BillingLimitService>>();
        await using var context = new VocabularyServiceContext(_dbOptions);
        
        var entitlements = new BillingEntitlements("free", new System.Collections.Generic.Dictionary<string, string>());
        billingClientMock
            .Setup(x => x.GetEntitlementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlements);

        var service = new BillingLimitService(billingClientMock.Object, context, redisMock.Object, loggerMock.Object);

        // Act
        var result = await service.GetMaxCardsAsync(Guid.NewGuid());

        // Assert
        result.Should().Be(500);
    }
}
