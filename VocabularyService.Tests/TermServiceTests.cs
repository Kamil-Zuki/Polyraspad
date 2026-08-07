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

public class TermServiceTests
{
    [Fact]
    public async Task CreateOrUpdateAsync_DefaultHint_PersistsSavedStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            AddProject(arrange, userId, projectId);
            await arrange.SaveChangesAsync();
        }

        await using var ctx = CreateContext(dbName);
        var sut = new TermService(ctx, NullLogger<TermService>.Instance);

        var status = await sut.CreateOrUpdateAsync(
            userId,
            projectId,
            "  Slept  ",
            "WORD",
            "en",
            statusHint: null,
            meaning: "спал",
            firstSentence: "I slept well.",
            firstSourceTitle: null,
            firstSourceUrl: null);

        var term = await ctx.ProjectTerms.SingleAsync();
        term.Text.Should().Be("Slept");
        term.NormalizedText.Should().Be("slept");
        term.Type.Should().Be("WORD");
        term.Language.Should().Be("en");
        status.ProjectTermId.Should().Be(term.Id);
        status.Status.Should().Be("SAVED");
        status.Meaning.Should().Be("спал");
        status.FirstSentence.Should().Be("I slept well.");
        (await ctx.Cards.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_LegacyLingqHint_StoresSaved()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            AddProject(arrange, userId, projectId);
            await arrange.SaveChangesAsync();
        }

        await using var ctx = CreateContext(dbName);
        var sut = new TermService(ctx, NullLogger<TermService>.Instance);

        var status = await sut.CreateOrUpdateAsync(
            userId,
            projectId,
            "hello",
            "WORD",
            "en",
            statusHint: "LINGQ",
            meaning: "привет",
            firstSentence: null,
            firstSourceTitle: null,
            firstSourceUrl: null);

        status.Status.Should().Be("SAVED");
    }

    [Fact]
    public async Task CreateOrUpdateAsync_GoAndWent_AreSeparateTerms()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            AddProject(arrange, userId, projectId);
            await arrange.SaveChangesAsync();
        }

        await using var ctx = CreateContext(dbName);
        var sut = new TermService(ctx, NullLogger<TermService>.Instance);

        await sut.CreateOrUpdateAsync(userId, projectId, "go", "WORD", "en", "KNOWN", null, null, null, null);
        await sut.CreateOrUpdateAsync(userId, projectId, "went", "WORD", "en", "SAVED", "шёл", null, null, null);

        (await ctx.ProjectTerms.CountAsync()).Should().Be(2);

        var (_, goStatus) = await sut.GetDetailsAsync(userId, projectId, "go", "WORD");
        var (_, wentStatus) = await sut.GetDetailsAsync(userId, projectId, "went", "WORD");

        goStatus!.Status.Should().Be("KNOWN");
        wentStatus!.Status.Should().Be("SAVED");
    }

    [Fact]
    public async Task ListProjectTermsAsync_FilterNew_ReturnsOnlyNewTerms()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            AddProject(arrange, userId, projectId);
            await arrange.SaveChangesAsync();
        }

        await using var ctx = CreateContext(dbName);
        var sut = new TermService(ctx, NullLogger<TermService>.Instance);

        await sut.CreateOrUpdateAsync(userId, projectId, "alpha", "WORD", "en", "NEW", null, null, null, null);
        await sut.CreateOrUpdateAsync(userId, projectId, "beta", "WORD", "en", "SAVED", "x", null, null, null);

        var list = await sut.ListProjectTermsAsync(userId, projectId, "NEW", null, null, null, 50, default);

        list.Items.Should().ContainSingle();
        list.Items[0].Text.Should().Be("alpha");
        list.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task ListProjectTermsAsync_PageSizeOne_ReturnsNextCursor()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            AddProject(arrange, userId, projectId);
            await arrange.SaveChangesAsync();
        }

        await using var ctx = CreateContext(dbName);
        var sut = new TermService(ctx, NullLogger<TermService>.Instance);

        await sut.CreateOrUpdateAsync(userId, projectId, "alpha", "WORD", "en", "NEW", null, null, null, null);
        await sut.CreateOrUpdateAsync(userId, projectId, "beta", "WORD", "en", "SAVED", "x", null, null, null);

        var first = await sut.ListProjectTermsAsync(userId, projectId, null, null, null, null, 1, default);
        first.Items.Should().HaveCount(1);
        first.NextCursor.Should().NotBeNullOrWhiteSpace();

        var second = await sut.ListProjectTermsAsync(userId, projectId, null, null, null, first.NextCursor, 10, default);
        second.Items.Should().HaveCount(1);
        second.Items[0].Text.Should().NotBe(first.Items[0].Text);
    }

    [Fact]
    public async Task BulkMarkKnownAsync_MarksSeveralSurfaces_AsKnown()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            AddProject(arrange, userId, projectId);
            await arrange.SaveChangesAsync();
        }

        await using var ctx = CreateContext(dbName);
        var sut = new TermService(ctx, NullLogger<TermService>.Instance);

        var updated = await sut.BulkMarkKnownAsync(
            userId,
            projectId,
            [new("alpha"), new("beta")],
            "en");

        updated.Should().Be(2);

        var (_, alpha) = await sut.GetDetailsAsync(userId, projectId, "alpha", "WORD");
        var (_, beta) = await sut.GetDetailsAsync(userId, projectId, "beta", "WORD");
        alpha!.Status.Should().Be("KNOWN");
        beta!.Status.Should().Be("KNOWN");
    }

    [Fact]
    public async Task PurgeDemoImportDataAsync_RemovesDemoCardsAndStatuses_KeepsRealMemoryTerm()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            AddProject(arrange, userId, projectId);
            AddDeck(arrange, userId, projectId, deckId);
            await arrange.SaveChangesAsync();

            var cardService = CreateCardService(arrange);
            await cardService.CreateCardAsync(new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new()
                    {
                        String = "[Import demo #1] Practice the word \"memory\" in context today.",
                    },
                    [SentenceMiningNoteType.Word] = new() { String = "memory" },
                    [SentenceMiningNoteType.Translation] = new() { String = "демо-memory-1" },
                },
            });
            await cardService.CreateCardAsync(new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new() { String = "An apple a day keeps the doctor away." },
                    [SentenceMiningNoteType.Word] = new() { String = "apple" },
                    [SentenceMiningNoteType.Translation] = new() { String = "яблоко" },
                },
            });
        }

        await using var ctx = CreateContext(dbName);
        var sut = new TermService(ctx, NullLogger<TermService>.Instance);

        var result = await sut.PurgeDemoImportDataAsync(userId, projectId);

        result.CardsDeleted.Should().Be(1);
        result.StatusesDeleted.Should().Be(1);
        result.TermsDeleted.Should().Be(1);

        (await ctx.Cards.CountAsync()).Should().Be(1);
        (await ctx.UserTermStatuses.CountAsync()).Should().Be(1);
        (await ctx.ProjectTerms.CountAsync()).Should().Be(1);

        var remainingStatus = await ctx.UserTermStatuses.SingleAsync();
        remainingStatus.Meaning.Should().Be("яблоко");
        remainingStatus.FirstSentence.Should().Be("An apple a day keeps the doctor away.");
    }

    private static void AddProject(VocabularyServiceContext context, Guid userId, Guid projectId)
    {
        context.Projects.Add(new Project
        {
            Id = projectId,
            UserId = userId,
            Title = "English",
            SourceLang = "en",
            TargetLang = "ru",
            FsrsSettings = new FsrsSettings(),
            Stats = new ProjectStats(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private static void AddDeck(
        VocabularyServiceContext context,
        Guid userId,
        Guid projectId,
        Guid deckId)
    {
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
            UpdatedAt = DateTime.UtcNow,
        });
    }

    private static CardService CreateCardService(VocabularyServiceContext context) =>
        new(
            context,
            new TermService(context, NullLogger<TermService>.Instance),
            new StubMediaService(),
            new NoteTypeService(context),
            NullLogger<CardService>.Instance);

    private sealed class StubMediaService : IMediaService
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

    private static VocabularyServiceContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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
            modelBuilder.Entity<Card>().Ignore(card => card.SearchVector);
        }
    }
}
