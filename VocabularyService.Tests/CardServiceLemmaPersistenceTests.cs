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

public class CardServiceLemmaPersistenceTests
{
    [Fact]
    public async Task CreateCardAsync_WhenLemmaNeedsPersistedMainCard_ShouldBackfillMainCardAfterSave()
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

        Card createdCard;
        await using (var actContext = CreateContext(dbName))
        {
            var sut = new CardService(
                actContext,
                new PersistedMainCardLemmaService(actContext),
                new StubMediaService(),
                NullLogger<CardService>.Instance);

            createdCard = await sut.CreateCardAsync(new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                Sentence = "I save the word apple",
                TargetWord = "apple",
                Translation = "яблоко"
            });
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var lemma = await assertContext.ProjectLemmas.AsNoTracking().SingleAsync();
            var deck = await assertContext.Decks.AsNoTracking().SingleAsync(d => d.Id == deckId);
            var card = await assertContext.Cards.AsNoTracking().SingleAsync(c => c.Id == createdCard.Id);

            card.LemmaId.Should().Be(lemma.Id);
            lemma.MainCardId.Should().Be(createdCard.Id);
            deck.CardCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task BulkCreateCardsAsync_WhenMultipleCardsShareNewLemma_ShouldPersistOnceAndBackfillFirstCard()
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

        List<Card> createdCards;
        await using (var actContext = CreateContext(dbName))
        {
            var sut = new CardService(
                actContext,
                new PersistedMainCardLemmaService(actContext),
                new StubMediaService(),
                NullLogger<CardService>.Instance);

            createdCards = await sut.BulkCreateCardsAsync(
                userId,
                deckId,
                [
                    new CreateCardDto
                    {
                        UserId = userId,
                        DeckId = deckId,
                        Sentence = "First apple sentence",
                        TargetWord = "apple",
                        Translation = "яблоко"
                    },
                    new CreateCardDto
                    {
                        UserId = userId,
                        DeckId = deckId,
                        Sentence = "Second apple sentence",
                        TargetWord = "apple",
                        Translation = "яблоко"
                    }
                ]);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var lemma = await assertContext.ProjectLemmas.AsNoTracking().SingleAsync();
            var deck = await assertContext.Decks.AsNoTracking().SingleAsync(d => d.Id == deckId);
            var cards = await assertContext.Cards.AsNoTracking().OrderBy(c => c.CreatedAt).ToListAsync();

            cards.Should().HaveCount(2);
            cards.Select(card => card.LemmaId).Distinct().Should().ContainSingle().Which.Should().Be(lemma.Id);
            lemma.MainCardId.Should().Be(createdCards[0].Id);
            deck.CardCount.Should().Be(2);
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

    private sealed class TestVocabularyServiceContext : VocabularyServiceContext
    {
        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Card>().Ignore(card => card.SearchVector);
        }
    }

    private sealed class StubMediaService : IMediaService
    {
        public Task<Guid> UploadImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task FillCardMediaUrlsAsync(CardMedia? media, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class PersistedMainCardLemmaService(VocabularyServiceContext context) : ILemmaService
    {
        public string Normalize(string word) => word.Trim().ToLowerInvariant();

        public async Task<ProjectLemma?> ResolveForCardAsync(
            Guid projectId,
            string targetWord,
            Guid? mainCardId = null,
            CancellationToken cancellationToken = default)
        {
            if (mainCardId.HasValue
                && !await context.Cards.AnyAsync(card => card.Id == mainCardId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Main card must be persisted before assigning it to a lemma.");
            }

            var lemmaText = Normalize(targetWord);
            if (string.IsNullOrWhiteSpace(lemmaText))
            {
                return null;
            }

            var existing = context.ProjectLemmas.Local
                .FirstOrDefault(lemma => lemma.ProjectId == projectId && lemma.Text == lemmaText)
                ?? await context.ProjectLemmas.FirstOrDefaultAsync(
                    lemma => lemma.ProjectId == projectId && lemma.Text == lemmaText,
                    cancellationToken);

            if (existing != null)
            {
                if (!existing.MainCardId.HasValue && mainCardId.HasValue)
                {
                    existing.MainCardId = mainCardId;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                return existing;
            }

            var created = new ProjectLemma
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = lemmaText,
                Status = "LEARNING",
                MainCardId = mainCardId,
                UpdatedAt = DateTime.UtcNow
            };

            context.ProjectLemmas.Add(created);
            return created;
        }
    }
}
