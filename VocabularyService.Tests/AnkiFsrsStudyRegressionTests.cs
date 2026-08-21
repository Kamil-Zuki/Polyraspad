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

/// <summary>
/// Anki FSRS regression tests (scripted scheduler; production uses inclusive py-fsrs).
/// </summary>
public class AnkiFsrsStudyRegressionTests
{
    [Fact]
    public async Task Preview_NewCard_Good_ShowsOneMinuteThenTenMinuteThenDay()
    {
        var now = DateTime.UtcNow;
        var fsrs = CreateLearningLadderScheduler();
        var preview = new FsrsPreviewService(fsrs);

        var newProgress = new UserCardProgress
        {
            UserId = Guid.NewGuid(),
            CardId = Guid.NewGuid(),
            State = 0,
            Step = 0,
            Due = now,
            LastReview = now,
        };

        var settings = new FsrsSettings { LearningStepsSeconds = [60, 600], EnableFuzzing = true };

        var first = await preview.GetButtonIntervalsAsync(newProgress, settings, CancellationToken.None);
        first[3].Should().Be("1m");

        var afterFirstGood = await ApplyScriptedAsync(fsrs, newProgress, 3, now);
        var second = await preview.GetButtonIntervalsAsync(afterFirstGood, settings, CancellationToken.None);
        second[3].Should().Be("10m");

        var afterSecondGood = await ApplyScriptedAsync(fsrs, afterFirstGood, 3, now);

        var third = await preview.GetButtonIntervalsAsync(afterSecondGood, settings, CancellationToken.None);
        third[3].Should().Be("1d");
    }

    [Fact]
    public async Task SubmitReview_GoodOnLearning_SchedulesTimedLearningQueue_NotDueList()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        Guid cardId;
        var now = DateTime.UtcNow;
        var redis = RedisTestHelper.CreateConnectionMultiplexer();

        await using var ctx = new TestVocabularyServiceContext(options);
        SeedProjectDeck(ctx, userId, projectId, deckId, now);
        await ctx.SaveChangesAsync();
        cardId = await CreateMiningCardAsync(ctx, userId, deckId, "word", "word");
        ctx.UserCardProgresses.Add(new UserCardProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardId = cardId,
            ProjectId = projectId,
            State = 0,
            Step = 0,
            Due = now,
            LastReview = now,
            Reps = 0,
            Lapses = 0,
            IsSuspended = false,
        });
        await ctx.SaveChangesAsync();
        var fsrs = CreateLearningLadderScheduler();
        var sut = BuildStudyService(ctx, userId, now, fsrs, redis);
        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);

        var review = await sut.SubmitReviewAsync(session.Id, userId, cardId, 3, 1200, null, CancellationToken.None);
        review.State.Should().Be("LEARNING");
        review.Interval.Should().Be("1m");

        var db = redis.GetDatabase();
        var learningKey = $"study:session:{session.Id}:learning";
        var dueKey = $"study:session:{session.Id}:due";
        (await db.SortedSetScoreAsync(learningKey, cardId.ToString())).Should().NotBeNull();
        (await db.ListLengthAsync(dueKey)).Should().Be(0);
    }

    [Fact]
    public async Task UndoReview_RestoresProgressFieldsAndRequeuesCard()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        Guid cardId;
        var now = DateTime.UtcNow;

        await using var ctx = new TestVocabularyServiceContext(options);
        SeedProjectDeck(ctx, userId, projectId, deckId, now);
        await ctx.SaveChangesAsync();
        cardId = await CreateMiningCardAsync(ctx, userId, deckId, "undo", "undo");
        ctx.UserCardProgresses.Add(new UserCardProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardId = cardId,
            ProjectId = projectId,
            State = 2,
            Step = 0,
            Stability = 10f,
            Difficulty = 5f,
            Due = now.AddDays(-1),
            LastReview = now.AddDays(-2),
            ElapsedDays = 1,
            ScheduledDays = 1,
            Reps = 3,
            Lapses = 1,
            IsSuspended = false,
        });
        await ctx.SaveChangesAsync();
        var fsrs = CreateLearningLadderScheduler();
        var sut = BuildStudyService(ctx, userId, now, fsrs, RedisTestHelper.CreateConnectionMultiplexer());
        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);

        await sut.SubmitReviewAsync(session.Id, userId, cardId, 1, 500, null, CancellationToken.None);
        await sut.UndoReviewAsync(session.Id, userId, CancellationToken.None);

        var progress = await ctx.UserCardProgresses.SingleAsync(p => p.CardId == cardId && p.ProjectId == projectId);
        progress.State.Should().Be(2);
        progress.Reps.Should().Be(3);
        progress.Lapses.Should().Be(1);
        progress.ElapsedDays.Should().Be(1);
        progress.ScheduledDays.Should().Be(1);
        (await ctx.ReviewLogs.CountAsync()).Should().Be(0);
    }

    private static IFsrsScheduler CreateLearningLadderScheduler() =>
        new ScriptedLearningLadderFsrs();

    private static async Task<UserCardProgress> ApplyScriptedAsync(
        IFsrsScheduler fsrs,
        UserCardProgress progress,
        int rating,
        DateTime reviewAt)
    {
        var clone = Clone(progress);
        var next = await fsrs.GetNextStateAsync(clone, rating, reviewAt, 0, null);
        clone.State = next.State;
        clone.Step = next.Step;
        clone.Stability = next.Stability;
        clone.Difficulty = next.Difficulty;
        clone.Due = next.Due;
        clone.LastReview = reviewAt;
        clone.Reps += 1;
        return clone;
    }

    private static UserCardProgress Clone(UserCardProgress source) => new()
    {
        UserId = source.UserId,
        CardId = source.CardId,
        ProjectId = source.ProjectId,
        State = source.State,
        Step = source.Step,
        Stability = source.Stability,
        Difficulty = source.Difficulty,
        Due = source.Due,
        LastReview = source.LastReview,
        Reps = source.Reps,
        Lapses = source.Lapses,
        ElapsedDays = source.ElapsedDays,
        ScheduledDays = source.ScheduledDays,
    };

    private static StudyService BuildStudyService(
        VocabularyServiceContext ctx,
        Guid userId,
        DateTime now,
        IFsrsScheduler fsrs,
        StackExchange.Redis.IConnectionMultiplexer redis)
    {
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

        var media = new Mock<IMediaService>(MockBehavior.Strict);
        media
            .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardService = new CardService(
            ctx,
            new TermService(ctx, NullLogger<TermService>.Instance),
            media.Object,
            new NoteTypeService(ctx),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<CardService>>());

        return StudyServiceTestFactory.Create(ctx, cardService, fsrs, userSettingsMock.Object, media.Object, redis);
    }

    private static void SeedProjectDeck(
        VocabularyServiceContext ctx,
        Guid userId,
        Guid projectId,
        Guid deckId,
        DateTime now)
    {
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
    }

    private static async Task<Guid> CreateMiningCardAsync(
        VocabularyServiceContext ctx,
        Guid userId,
        Guid deckId,
        string expression,
        string word)
    {
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
                [SentenceMiningNoteType.Expression] = new() { String = expression },
                [SentenceMiningNoteType.Word] = new() { String = word },
                [SentenceMiningNoteType.Translation] = new() { String = "t" },
            },
        });
        return card.Id;
    }

    private sealed class ScriptedLearningLadderFsrs : IFsrsScheduler
    {
        public Task<FsrsNextState> GetNextStateAsync(
            UserCardProgress progress,
            int rating,
            DateTime reviewAt,
            int durationMs,
            FsrsSettings? settings,
            CancellationToken cancellationToken = default)
        {
            if (rating != 3)
            {
                return Task.FromResult(new FsrsNextState(1f, 5f, reviewAt.AddMinutes(1), 1, 0));
            }

            if (progress.State == 0)
            {
                return Task.FromResult(new FsrsNextState(1f, 5f, reviewAt.AddMinutes(1), 1, 1));
            }

            if (progress.State == 1 && progress.Step <= 1)
            {
                return Task.FromResult(new FsrsNextState(2f, 5f, reviewAt.AddMinutes(10), 1, 2));
            }

            return Task.FromResult(new FsrsNextState(10f, 5f, reviewAt.AddDays(1), 2, 0));
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
