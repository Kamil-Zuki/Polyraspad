using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Dtos.Cards;
using VocabularyService.Helpers;
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
            var sut = new CardService(
                actContext,
                new TermService(actContext, NullLogger<TermService>.Instance),
                new MockMediaStorage(),
                new NoteTypeService(actContext),
                NullLogger<CardService>.Instance);

            var dto = new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new() { String = "I like apples" },
                    [SentenceMiningNoteType.Word] = new() { String = "apples" },
                    [SentenceMiningNoteType.Translation] = new() { String = "Я люблю яблоки" },
                    [SentenceMiningNoteType.Image] = new() { String = imageUrl },
                    [SentenceMiningNoteType.Audio] = new() { String = audioUrl },
                },
            };

            var created = await sut.CreateCardAsync(dto);
            createdCardId = created.Id;
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var fromDb = await assertContext.Cards.AsNoTracking()
                .Include(c => c.Note)
                .SingleAsync(c => c.Id == createdCardId);
            fromDb.Note.Should().NotBeNull();
            var media = NoteFieldMapHelper.BuildCardMedia(fromDb.Note!.FieldValues);
            media.Should().NotBeNull();
            media!.ImageUrl.Should().Be(imageUrl);
            media.AudioUrl.Should().Be(audioUrl);
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
            var sut = new CardService(
                actContext,
                new TermService(actContext, NullLogger<TermService>.Instance),
                new MockMediaStorage(),
                new NoteTypeService(actContext),
                NullLogger<CardService>.Instance);

            var dto = new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new() { String = "I like apples" },
                    [SentenceMiningNoteType.Word] = new() { String = "apples" },
                    [SentenceMiningNoteType.Translation] = new() { String = "Я люблю яблоки" },
                },
            };

            var created = await sut.CreateCardAsync(dto);
            createdCardId = created.Id;
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var fromDb = await assertContext.Cards.AsNoTracking()
                .Include(c => c.Note)
                .SingleAsync(c => c.Id == createdCardId);
            NoteFieldMapHelper.BuildCardMedia(fromDb.Note!.FieldValues).Should().BeNull();
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
            var sut = new CardService(
                actContext,
                new TermService(actContext, NullLogger<TermService>.Instance),
                new MockMediaStorage(),
                new NoteTypeService(actContext),
                NullLogger<CardService>.Instance);

            var dtos = new List<CreateCardDto>
            {
                new()
                {
                    UserId = userId,
                    DeckId = deckId,
                    FieldValues = new Dictionary<string, NoteFieldValue>
                    {
                        [SentenceMiningNoteType.Expression] = new() { String = "I like apples" },
                        [SentenceMiningNoteType.Word] = new() { String = "apples" },
                        [SentenceMiningNoteType.Translation] = new() { String = "Я люблю яблоки" },
                        [SentenceMiningNoteType.Image] = new() { String = imageUrl1 },
                        [SentenceMiningNoteType.Audio] = new() { String = audioUrl1 },
                    },
                },
                new()
                {
                    UserId = userId,
                    DeckId = deckId,
                    FieldValues = new Dictionary<string, NoteFieldValue>
                    {
                        [SentenceMiningNoteType.Expression] = new() { String = "I like bananas" },
                        [SentenceMiningNoteType.Word] = new() { String = "bananas" },
                        [SentenceMiningNoteType.Translation] = new() { String = "Я люблю бананы" },
                        [SentenceMiningNoteType.Audio] = new() { String = audioUrl2 },
                    },
                },
            };

            var created = await sut.BulkCreateCardsAsync(userId, deckId, dtos);
            createdIds = created.Select(c => c.Id).ToList();
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var cards = await assertContext.Cards.AsNoTracking()
                .Include(c => c.Note)
                .Where(c => createdIds.Contains(c.Id))
                .OrderBy(c => NoteFieldMapHelper.GetWord(c.Note!.FieldValues))
                .ToListAsync();

            cards.Should().HaveCount(2);

            var apples = cards.Single(c => NoteFieldMapHelper.GetWord(c.Note!.FieldValues) == "apples");
            var mApples = NoteFieldMapHelper.BuildCardMedia(apples.Note!.FieldValues);
            mApples.Should().NotBeNull();
            mApples!.ImageUrl.Should().Be(imageUrl1);
            mApples.AudioUrl.Should().Be(audioUrl1);

            var bananas = cards.Single(c => NoteFieldMapHelper.GetWord(c.Note!.FieldValues) == "bananas");
            var mBananas = NoteFieldMapHelper.BuildCardMedia(bananas.Note!.FieldValues);
            mBananas.Should().NotBeNull();
            mBananas!.ImageUrl.Should().BeNull();
            mBananas.AudioUrl.Should().Be(audioUrl2);
        }
    }

    [Fact]
    public async Task CaptureCardAsync_WhenScreenshotBase64Provided_ShouldUploadAndSetImageId()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var expectedImageId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
        {
            ArrangeProjectAndDeck(arrangeContext, userId, projectId, Guid.NewGuid());
            await arrangeContext.SaveChangesAsync();
        }

        var screenshotBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="; // 1x1 PNG

        Guid createdCardId;
        await using (var actContext = CreateContext(dbName))
        {
            var mockStorage = new Mock<IMediaService>();
            mockStorage
                .Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedImageId);

            var sut = new CardService(
                actContext,
                new TermService(actContext, NullLogger<TermService>.Instance),
                mockStorage.Object,
                new NoteTypeService(actContext),
                NullLogger<CardService>.Instance);

            var dto = new CaptureCardDto
            {
                UserId = userId,
                ProjectId = projectId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new() { String = "Test sentence" },
                    [SentenceMiningNoteType.Word] = new() { String = "sentence" },
                    [SentenceMiningNoteType.Translation] = new() { String = "Тест" },
                },
                ScreenshotBase64 = screenshotBase64,
            };

            var created = await sut.CaptureCardAsync(dto);
            createdCardId = created.Id;
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var fromDb = await assertContext.Cards.AsNoTracking()
                .Include(c => c.Note)
                .SingleAsync(c => c.Id == createdCardId);
            fromDb.Note!.FieldValues[SentenceMiningNoteType.Image].String.Should().Be(expectedImageId.ToString());
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

    private sealed class MockMediaStorage : IMediaService
    {
        public Task<Guid> UploadImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task<Guid> UploadAudioAsync(Stream data, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task<string> GetDocumentUrlAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task FillCardMediaUrlsAsync(CardMedia? media, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
