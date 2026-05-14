using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Dtos.Cards;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class CardServiceTermPersistenceTests
{
    [Fact]
    public async Task CreateCardAsync_ShouldAssignProjectTermToNewCards()
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
                new LemmaService(actContext, NullLogger<LemmaService>.Instance),
                new TermService(actContext, NullLogger<TermService>.Instance),
                new StubMediaService(),
                new NoteTypeService(actContext),
                NullLogger<CardService>.Instance);

            createdCard = await sut.CreateCardAsync(new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new() { String = "I save the word apple" },
                    [SentenceMiningNoteType.Word] = new() { String = "apple" },
                    [SentenceMiningNoteType.Translation] = new() { String = "яблоко" },
                },
            });
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var deck = await assertContext.Decks.AsNoTracking().SingleAsync(d => d.Id == deckId);
            var card = await assertContext.Cards.AsNoTracking().SingleAsync(c => c.Id == createdCard.Id);

            card.ProjectTermId.Should().NotBeNull();
            (await assertContext.ProjectTerms.AsNoTracking().CountAsync()).Should().Be(1);
            (await assertContext.UserTermStatuses.AsNoTracking().CountAsync()).Should().Be(1);
            deck.CardCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task BulkCreateCardsAsync_ShouldAssignTermsToNewCards()
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

        await using (var actContext = CreateContext(dbName))
        {
            var sut = new CardService(
                actContext,
                new LemmaService(actContext, NullLogger<LemmaService>.Instance),
                new TermService(actContext, NullLogger<TermService>.Instance),
                new StubMediaService(),
                new NoteTypeService(actContext),
                NullLogger<CardService>.Instance);

            await sut.BulkCreateCardsAsync(
                userId,
                deckId,
                [
                    new CreateCardDto
                    {
                        UserId = userId,
                        DeckId = deckId,
                        FieldValues = new Dictionary<string, NoteFieldValue>
                        {
                            [SentenceMiningNoteType.Expression] = new() { String = "First apple sentence" },
                            [SentenceMiningNoteType.Word] = new() { String = "apple" },
                            [SentenceMiningNoteType.Translation] = new() { String = "яблоко" },
                        },
                    },
                    new CreateCardDto
                    {
                        UserId = userId,
                        DeckId = deckId,
                        FieldValues = new Dictionary<string, NoteFieldValue>
                        {
                            [SentenceMiningNoteType.Expression] = new() { String = "Second apple sentence" },
                            [SentenceMiningNoteType.Word] = new() { String = "apple" },
                            [SentenceMiningNoteType.Translation] = new() { String = "яблоко" },
                        },
                    }
                ]);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var deck = await assertContext.Decks.AsNoTracking().SingleAsync(d => d.Id == deckId);
            var cards = await assertContext.Cards.AsNoTracking().OrderBy(c => c.CreatedAt).ToListAsync();

            cards.Should().HaveCount(2);
            cards.Should().OnlyContain(card => card.ProjectTermId != null);
            (await assertContext.ProjectTerms.AsNoTracking().CountAsync()).Should().Be(1);
            (await assertContext.UserTermStatuses.AsNoTracking().CountAsync()).Should().Be(1);
            deck.CardCount.Should().Be(2);
        }
    }

    [Fact]
    public async Task CheckDuplicatesAsync_UsesExactRealTermInsteadOfLemma()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangeProjectAndDeck(arrangeContext, userId, projectId, deckId);
            await arrangeContext.SaveChangesAsync();

            var seed = new CardService(
                arrangeContext,
                new LemmaService(arrangeContext, NullLogger<LemmaService>.Instance),
                new TermService(arrangeContext, NullLogger<TermService>.Instance),
                new StubMediaService(),
                new NoteTypeService(arrangeContext),
                NullLogger<CardService>.Instance);
            await seed.CreateCardAsync(
                new CreateCardDto
                {
                    UserId = userId,
                    DeckId = deckId,
                    FieldValues = new Dictionary<string, NoteFieldValue>
                    {
                        [SentenceMiningNoteType.Expression] = new() { String = "I go home" },
                        [SentenceMiningNoteType.Word] = new() { String = "go" },
                        [SentenceMiningNoteType.Translation] = new() { String = "идти" },
                    },
                });
        }

        await using var actContext = CreateContext(dbName);
        var sut = new CardService(
            actContext,
            new LemmaService(actContext, NullLogger<LemmaService>.Instance),
            new TermService(actContext, NullLogger<TermService>.Instance),
            new StubMediaService(),
            new NoteTypeService(actContext),
            NullLogger<CardService>.Instance);

        var went = await sut.CheckDuplicatesAsync(
            userId,
            new CheckCardDuplicatesRequestDto
            {
                ProjectId = projectId,
                TargetWord = "went"
            });
        var go = await sut.CheckDuplicatesAsync(
            userId,
            new CheckCardDuplicatesRequestDto
            {
                ProjectId = projectId,
                TargetWord = "Go"
            });

        went.IsDuplicate.Should().BeFalse();
        go.IsDuplicate.Should().BeTrue();
        go.ExistingCards.Should().ContainSingle(card => card.TargetWord == "go");
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

}


