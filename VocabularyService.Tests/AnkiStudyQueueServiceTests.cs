#nullable enable
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VocabularyService.Data;
using VocabularyService.Services.Study;
using Xunit;

namespace VocabularyService.Tests;

public class AnkiStudyQueueServiceTests
{
    [Fact]
    public async Task InitializeDueQueueAsync_DoesNotMirrorIntoLegacyQueue()
    {
        var sessionId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var redis = RedisTestHelper.CreateConnectionMultiplexer();
        await using var ctx = CreateContext();
        var sut = new AnkiStudyQueueService(ctx, redis);

        await sut.InitializeDueQueueAsync(sessionId, [cardId]);

        var db = redis.GetDatabase();
        var dueKey = DueQueueKey(sessionId);
        var legacyKey = LegacyQueueKey(sessionId);

        (await db.ListLengthAsync(dueKey)).Should().Be(1);
        (await db.KeyExistsAsync(legacyKey)).Should().BeFalse();
    }

    [Fact]
    public async Task PopDueCardIdAsync_WhenDueDrained_DoesNotResurrectFromStaleLegacyQueue()
    {
        var sessionId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var redis = RedisTestHelper.CreateConnectionMultiplexer();
        await using var ctx = CreateContext();
        var sut = new AnkiStudyQueueService(ctx, redis);

        await sut.InitializeDueQueueAsync(sessionId, [cardId]);

        var db = redis.GetDatabase();
        var legacyKey = LegacyQueueKey(sessionId);
        await db.ListRightPushAsync(legacyKey, cardId.ToString());

        var first = await sut.PopDueCardIdAsync(sessionId);
        first.Should().Be(cardId);

        var second = await sut.PopDueCardIdAsync(sessionId);
        second.Should().BeNull();

        (await db.KeyExistsAsync(legacyKey)).Should().BeFalse();
    }

    [Fact]
    public async Task PopDueCardIdAsync_WhenOnlyLegacyQueueExists_MigratesOnce()
    {
        var sessionId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var redis = RedisTestHelper.CreateConnectionMultiplexer();
        await using var ctx = CreateContext();
        var sut = new AnkiStudyQueueService(ctx, redis);

        var db = redis.GetDatabase();
        var legacyKey = LegacyQueueKey(sessionId);
        await db.ListRightPushAsync(legacyKey, cardId.ToString());

        var first = await sut.PopDueCardIdAsync(sessionId);
        first.Should().Be(cardId);

        var second = await sut.PopDueCardIdAsync(sessionId);
        second.Should().BeNull();

        (await db.KeyExistsAsync(legacyKey)).Should().BeFalse();
    }

    private static string DueQueueKey(Guid sessionId) => $"study:session:{sessionId}:due";

    private static string LegacyQueueKey(Guid sessionId) => $"study:session:{sessionId}:queue";

    private static TestVocabularyServiceContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestVocabularyServiceContext(options);
    }

    private sealed class TestVocabularyServiceContext : VocabularyServiceContext
    {
        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options)
            : base(options) { }
    }
}
