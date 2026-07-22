#nullable enable
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
using VocabularyService.Services;
using VocabularyService.Services.Study;
using Xunit;

namespace VocabularyService.Tests;

public class StudyServiceKnownStatusSyncTests
{
    [Fact]
    public async Task SubmitReview_GoodToReview_SetsUserTermStatusKnown()
    {
        var (sut, ctx, userId, projectId, cardId, termId, sessionId) = await SeedAsync(
            initialProgressState: 1,
            initialTermStatus: "SAVED",
            nextState: 2);

        await sut.SubmitReviewAsync(sessionId, userId, cardId, rating: 3, durationMs: 1000, null, CancellationToken.None);

        var status = await ctx.UserTermStatuses.SingleAsync(t => t.UserId == userId && t.ProjectTermId == termId);
        status.Status.Should().Be("KNOWN");
    }

    [Fact]
    public async Task SubmitReview_Again_DoesNotDemoteKnownOrSaved()
    {
        var (sut, ctx, userId, projectId, cardId, termId, sessionId) = await SeedAsync(
            initialProgressState: 2,
            initialTermStatus: "SAVED",
            nextState: 1);

        await sut.SubmitReviewAsync(sessionId, userId, cardId, rating: 1, durationMs: 500, null, CancellationToken.None);

        var status = await ctx.UserTermStatuses.SingleAsync(t => t.UserId == userId && t.ProjectTermId == termId);
        status.Status.Should().Be("SAVED");
    }

    [Fact]
    public async Task SubmitReview_GoodToReview_CreatesKnownStatusWhenMissing()
    {
        var (sut, ctx, userId, projectId, cardId, termId, sessionId) = await SeedAsync(
            initialProgressState: 1,
            initialTermStatus: null,
            nextState: 2);

        await sut.SubmitReviewAsync(sessionId, userId, cardId, rating: 3, durationMs: 1000, null, CancellationToken.None);

        var status = await ctx.UserTermStatuses.SingleAsync(t => t.UserId == userId && t.ProjectTermId == termId);
        status.Status.Should().Be("KNOWN");
        status.ProjectId.Should().Be(projectId);
    }

    private static async Task<(
        StudyService Sut,
        VocabularyServiceContext Ctx,
        Guid UserId,
        Guid ProjectId,
        Guid CardId,
        Guid TermId,
        Guid SessionId)> SeedAsync(short initialProgressState, string? initialTermStatus, short nextState)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var redis = RedisTestHelper.CreateConnectionMultiplexer();

        var ctx = new TestVocabularyServiceContext(options);
        ctx.Projects.Add(new Project
        {
            Id = projectId,
            UserId = userId,
            Title = "P",
            SourceLang = "en",
            TargetLang = "ru",
            FsrsSettings = new FsrsSettings { LearningStepsSeconds = [60, 600] },
            Stats = new ProjectStats(),
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        });
        ctx.Decks.Add(new Deck
        {
            Id = deckId,
            ProjectId = projectId,
            OwnerId = userId,
            Title = "Deck",
            ContributionPolicy = "OPEN",
            LicenseType = "PRIVATE",
            CardCount = 0,
            IsPublic = false,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await ctx.SaveChangesAsync();

        var media = new Mock<IMediaService>();
        media
            .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var cardService = new CardService(
            ctx,
            new TermService(ctx, NullLogger<TermService>.Instance),
            media.Object,
            new NoteTypeService(ctx),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<CardService>>());

        var card = await cardService.CreateCardAsync(new CreateCardDto
        {
            UserId = userId,
            DeckId = deckId,
            FieldValues = new Dictionary<string, NoteFieldValue>
            {
                [SentenceMiningNoteType.Expression] = new() { String = "The cat sat." },
                [SentenceMiningNoteType.Word] = new() { String = "cat" },
                [SentenceMiningNoteType.Translation] = new() { String = "кот" },
            },
        });

        var termId = card.ProjectTermId ?? throw new InvalidOperationException("Expected ProjectTermId on mining card");

        // CreateCard usually seeds a UserTermStatus; align it to the scenario instead of inserting a duplicate.
        var existingStatus = await ctx.UserTermStatuses
            .FirstOrDefaultAsync(t => t.UserId == userId && t.ProjectTermId == termId);
        if (initialTermStatus != null)
        {
            if (existingStatus != null)
            {
                existingStatus.Status = initialTermStatus;
                existingStatus.UpdatedAt = now;
            }
            else
            {
                ctx.UserTermStatuses.Add(new UserTermStatus
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProjectId = projectId,
                    ProjectTermId = termId,
                    Status = initialTermStatus,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }
        else if (existingStatus != null)
        {
            ctx.UserTermStatuses.Remove(existingStatus);
        }

        ctx.UserCardProgresses.Add(new UserCardProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardId = card.Id,
            ProjectId = projectId,
            State = initialProgressState,
            Step = 1,
            Due = now,
            LastReview = now,
            Reps = 1,
            Lapses = 0,
            IsSuspended = false,
        });
        await ctx.SaveChangesAsync();

        var userSettingsMock = new Mock<IUserSettingsService>(MockBehavior.Strict);
        userSettingsMock
            .Setup(s => s.GetUserSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSetting
            {
                UserId = userId,
                DailyGoalNew = 20,
                DailyGoalReview = 100,
                InterfaceLanguage = "en",
                UpdatedAt = now,
            });

        var fsrs = new FixedNextStateFsrs(nextState);
        var sut = StudyServiceTestFactory.Create(ctx, cardService, fsrs, userSettingsMock.Object, media.Object, redis);
        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);

        return (sut, ctx, userId, projectId, card.Id, termId, session.Id);
    }

    private sealed class FixedNextStateFsrs(short nextState) : IFsrsScheduler
    {
        public Task<FsrsNextState> GetNextStateAsync(
            UserCardProgress progress,
            int rating,
            DateTime reviewAt,
            int durationMs,
            FsrsSettings? settings,
            CancellationToken cancellationToken = default)
        {
            var due = nextState == 2 ? reviewAt.AddDays(1) : reviewAt.AddMinutes(1);
            return Task.FromResult(new FsrsNextState(10f, 5f, due, nextState, 0));
        }
    }

    private sealed class TestVocabularyServiceContext : VocabularyServiceContext
    {
        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Card>().Ignore(c => c.SearchVector);
        }
    }
}
