using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using VocabularyService.Data;
using Microsoft.Data.Sqlite;
using VocabularyService.Data.Entities.JsonTypes;
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

    [Fact]
    public async Task GetUserUsageStatsAsync_ReturnsDefaultCountsForEmptyContext()
    {
        // Arrange
        var billingClientMock = new Mock<IBillingEntitlementClient>();
        var redisMock = new Mock<IConnectionMultiplexer>();
        var loggerMock = new Mock<ILogger<BillingLimitService>>();
        await using var context = new TestVocabularyServiceContext(_dbOptions);

        var userId = Guid.NewGuid();
        var service = new BillingLimitService(billingClientMock.Object, context, redisMock.Object, loggerMock.Object);

        // Act
        var stats = await service.GetUserUsageStatsAsync(userId);

        // Assert
        stats.Should().NotBeNull();
        stats.ProjectsUsed.Should().Be(0);
        stats.CardsUsed.Should().Be(0);
        stats.AiRequestsTodayUsed.Should().Be(0);
        stats.BooksUsed.Should().Be(0);
    }

    private sealed class TestVocabularyServiceContext : VocabularyServiceContext
    {
        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<VocabularyService.Data.Entities.Card>().Ignore(card => card.SearchVector);
        }
    }
}






