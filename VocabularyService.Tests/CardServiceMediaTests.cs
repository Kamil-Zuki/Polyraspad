using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Dtos.Cards;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class CardServiceMediaTests
{
    [Fact]
    public async Task CreateCardAsync_WhenUrlsProvided_ShouldPersistMediaUrls()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangeProjectAndDeck(arrangeContext, userId, projectId, deckId);
            await arrangeContext.SaveChangesAsync();
        }

        Guid createdCardId;
        var imageUrl = "https://cdn.example.com/cards/1.png";
        var audioUrl = "https://cdn.example.com/cards/1.mp3";

        await using (var actContext = CreateContext(dbName))
        {
            var sut = new CardService(actContext, NullLogger<CardService>.Instance);

            var dto = new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                Sentence = "I like apples",
                TargetWord = "apples",
                Translation = "Я люблю яблоки",
                ImageUrl = imageUrl,
                AudioUrl = audioUrl
            };

            var created = await sut.CreateCardAsync(dto);
            createdCardId = created.Id;
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var fromDb = await assertContext.Cards.AsNoTracking().SingleAsync(c => c.Id == createdCardId);
            fromDb.Media.Should().NotBeNull();
            fromDb.Media!.ImageUrl.Should().Be(imageUrl);
            fromDb.Media.AudioUrl.Should().Be(audioUrl);
        }
    }

    [Fact]
    public async Task CreateCardAsync_WhenNoUrlsProvided_ShouldKeepMediaNull()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangeProjectAndDeck(arrangeContext, userId, projectId, deckId);
            await arrangeContext.SaveChangesAsync();
        }

        Guid createdCardId;
        await using (var actContext = CreateContext(dbName))
        {
            var sut = new CardService(actContext, NullLogger<CardService>.Instance);

            var dto = new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                Sentence = "I like apples",
                TargetWord = "apples",
                Translation = "Я люблю яблоки",
                ImageUrl = null,
                AudioUrl = null
            };

            var created = await sut.CreateCardAsync(dto);
            createdCardId = created.Id;
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var fromDb = await assertContext.Cards.AsNoTracking().SingleAsync(c => c.Id == createdCardId);
            fromDb.Media.Should().BeNull();
        }
    }

    [Fact]
    public async Task BulkCreateCardsAsync_WhenUrlsProvided_ShouldPersistMediaUrlsPerCard()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangeProjectAndDeck(arrangeContext, userId, projectId, deckId);
            await arrangeContext.SaveChangesAsync();
        }

        var imageUrl1 = "https://cdn.example.com/cards/1.png";
        var audioUrl1 = "https://cdn.example.com/cards/1.mp3";
        var audioUrl2 = "https://cdn.example.com/cards/2.mp3";

        List<Guid> createdIds;
        await using (var actContext = CreateContext(dbName))
        {
            var sut = new CardService(actContext, NullLogger<CardService>.Instance);

            var dtos = new List<CreateCardDto>
            {
                new()
                {
                    UserId = userId,
                    DeckId = deckId,
                    Sentence = "I like apples",
                    TargetWord = "apples",
                    Translation = "Я люблю яблоки",
                    ImageUrl = imageUrl1,
                    AudioUrl = audioUrl1
                },
                new()
                {
                    UserId = userId,
                    DeckId = deckId,
                    Sentence = "I like bananas",
                    TargetWord = "bananas",
                    Translation = "Я люблю бананы",
                    ImageUrl = null,
                    AudioUrl = audioUrl2
                }
            };

            var created = await sut.BulkCreateCardsAsync(userId, deckId, dtos);
            createdIds = created.Select(c => c.Id).ToList();
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var cards = await assertContext.Cards.AsNoTracking()
                .Where(c => createdIds.Contains(c.Id))
                .OrderBy(c => c.Sentence)
                .ToListAsync();

            cards.Should().HaveCount(2);

            var apples = cards.Single(c => c.TargetWord == "apples");
            apples.Media.Should().NotBeNull();
            apples.Media!.ImageUrl.Should().Be(imageUrl1);
            apples.Media.AudioUrl.Should().Be(audioUrl1);

            var bananas = cards.Single(c => c.TargetWord == "bananas");
            bananas.Media.Should().NotBeNull();
            bananas.Media!.ImageUrl.Should().BeNull();
            bananas.Media.AudioUrl.Should().Be(audioUrl2);
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

            // InMemory provider can't map Npgsql-specific types like NpgsqlTsVector.
            modelBuilder.Entity<Card>().Ignore(c => c.SearchVector);
        }
    }

    private static void ArrangeProjectAndDeck(
        VocabularyServiceContext context,
        Guid userId,
        Guid projectId,
        Guid deckId)
    {
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

        context.Decks.Add(new Deck
        {
            Id = deckId,
            ProjectId = projectId,
            OwnerId = userId,
            Title = "Test Deck",
            Description = null,
            CoverImageUrl = null,
            IsPublic = false,
            ContributionPolicy = "OPEN",
            LicenseType = "PRIVATE",
            ForkedFromId = null,
            CardCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}

