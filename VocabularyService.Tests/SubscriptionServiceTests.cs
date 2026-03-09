using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class SubscriptionServiceTests
{
    [Fact]
    public async Task should_return_only_current_user_subscriptions_ordered_by_subscribed_at_desc_when_user_has_multiple_subscriptions()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangeProjectWithDecksAndSubscriptions(arrangeContext, userId, otherUserId, projectId);
            await arrangeContext.SaveChangesAsync();
        }

        await using (var actContext = CreateContext(dbName))
        {
            var sut = new SubscriptionService(actContext, NullLogger<SubscriptionService>.Instance);

            var result = await sut.ListAsync(userId);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(x => x.ProjectId == projectId);
            result.Select(x => x.DeckId).Should().OnlyContain(id => id != Guid.Empty);
            result.Should().BeInDescendingOrder(x => x.SubscribedAt);
        }
    }

    [Fact]
    public async Task should_create_subscription_when_deck_is_public_and_free_and_user_not_yet_subscribed()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangePublicDeck(arrangeContext, userId, projectId, deckId);
            await arrangeContext.SaveChangesAsync();
        }

        await using (var actContext = CreateContext(dbName))
        {
            var sut = new SubscriptionService(actContext, NullLogger<SubscriptionService>.Instance);

            var created = await sut.SubscribeAsync(userId, deckId);

            created.DeckId.Should().Be(deckId);
            created.ProjectId.Should().Be(projectId);
            created.Title.Should().Be("Public Deck");
            created.SubscribedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var fromDb = await assertContext.DeckSubscriptions
                .SingleAsync(s => s.UserId == userId && s.DeckId == deckId);

            fromDb.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task should_be_idempotent_and_not_create_duplicate_subscription_when_subscription_already_exists()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangePublicDeck(arrangeContext, userId, projectId, deckId);

            arrangeContext.DeckSubscriptions.Add(new DeckSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeckId = deckId,
                SubscribedAt = DateTime.UtcNow.AddDays(-1),
                LastSyncedVersion = 1,
                LastAccessedAt = DateTime.UtcNow.AddDays(-1)
            });

            await arrangeContext.SaveChangesAsync();
        }

        await using (var actContext = CreateContext(dbName))
        {
            var sut = new SubscriptionService(actContext, NullLogger<SubscriptionService>.Instance);

            var first = await sut.SubscribeAsync(userId, deckId);
            var second = await sut.SubscribeAsync(userId, deckId);

            second.DeckId.Should().Be(deckId);
            second.SubscribedAt.Should().Be(first.SubscribedAt);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var count = await assertContext.DeckSubscriptions
                .CountAsync(s => s.UserId == userId && s.DeckId == deckId);

            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task should_remove_existing_subscription_when_user_is_subscribed()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangePublicDeck(arrangeContext, userId, projectId, deckId);

            arrangeContext.DeckSubscriptions.Add(new DeckSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeckId = deckId,
                SubscribedAt = DateTime.UtcNow.AddDays(-1),
                LastSyncedVersion = 1,
                LastAccessedAt = DateTime.UtcNow.AddDays(-1)
            });

            await arrangeContext.SaveChangesAsync();
        }

        await using (var actContext = CreateContext(dbName))
        {
            var sut = new SubscriptionService(actContext, NullLogger<SubscriptionService>.Instance);

            await sut.UnsubscribeAsync(userId, deckId);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var exists = await assertContext.DeckSubscriptions
                .AnyAsync(s => s.UserId == userId && s.DeckId == deckId);

            exists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task should_succeed_without_error_when_user_is_not_subscribed()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangePublicDeck(arrangeContext, userId, projectId, deckId);
            await arrangeContext.SaveChangesAsync();
        }

        await using (var actContext = CreateContext(dbName))
        {
            var sut = new SubscriptionService(actContext, NullLogger<SubscriptionService>.Instance);

            var act = () => sut.UnsubscribeAsync(userId, deckId);

            await act.Should().NotThrowAsync();
        }
    }

    private static VocabularyServiceContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        return new TestVocabularyServiceContext(options);
    }

    private sealed class TestVocabularyServiceContext : VocabularyServiceContext
    {
        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Card>().Ignore(c => c.SearchVector);
        }
    }

    private static void ArrangeProjectWithDecksAndSubscriptions(
        VocabularyServiceContext context,
        Guid userId,
        Guid otherUserId,
        Guid projectId)
    {
        var deck1Id = Guid.NewGuid();
        var deck2Id = Guid.NewGuid();
        var foreignDeckId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId,
            UserId = userId,
            Title = "Test Project",
            SourceLang = "en",
            TargetLang = "ru",
            FsrsSettings = new FsrsSettings(),
            Stats = new ProjectStats(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Decks.AddRange(
            new Deck
            {
                Id = deck1Id,
                ProjectId = projectId,
                OwnerId = userId,
                Title = "Deck 1",
                Description = null,
                CoverImageUrl = null,
                IsPublic = true,
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                ForkedFromId = null,
                CardCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Deck
            {
                Id = deck2Id,
                ProjectId = projectId,
                OwnerId = userId,
                Title = "Deck 2",
                Description = null,
                CoverImageUrl = null,
                IsPublic = true,
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                ForkedFromId = null,
                CardCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Deck
            {
                Id = foreignDeckId,
                ProjectId = Guid.NewGuid(),
                OwnerId = otherUserId,
                Title = "Foreign Deck",
                Description = null,
                CoverImageUrl = null,
                IsPublic = true,
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                ForkedFromId = null,
                CardCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        context.DeckSubscriptions.AddRange(
            new DeckSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeckId = deck1Id,
                SubscribedAt = DateTime.UtcNow.AddDays(-1),
                LastSyncedVersion = 1,
                LastAccessedAt = DateTime.UtcNow.AddDays(-1)
            },
            new DeckSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeckId = deck2Id,
                SubscribedAt = DateTime.UtcNow,
                LastSyncedVersion = 1,
                LastAccessedAt = DateTime.UtcNow
            },
            new DeckSubscription
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                DeckId = foreignDeckId,
                SubscribedAt = DateTime.UtcNow,
                LastSyncedVersion = 1,
                LastAccessedAt = DateTime.UtcNow
            });
    }

    private static void ArrangePublicDeck(
        VocabularyServiceContext context,
        Guid ownerId,
        Guid projectId,
        Guid deckId)
    {
        context.Projects.Add(new Project
        {
            Id = projectId,
            UserId = ownerId,
            Title = "Test Project",
            SourceLang = "en",
            TargetLang = "ru",
            FsrsSettings = new FsrsSettings(),
            Stats = new ProjectStats(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Decks.Add(new Deck
        {
            Id = deckId,
            ProjectId = projectId,
            OwnerId = ownerId,
            Title = "Public Deck",
            Description = null,
            CoverImageUrl = null,
            IsPublic = true,
            ContributionPolicy = "OPEN",
            LicenseType = "PRIVATE",
            ForkedFromId = null,
            CardCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}

