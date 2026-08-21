#nullable enable
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Dtos.Cards;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class StudyServiceStartSessionQueueTests
{
    [Fact]
    public async Task StartStudySession_Then_GetNextCard_ReturnsCard_When_OnlyLearnAheadEligible()
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
        Guid cardId;

        await using (var arrange = new TestVocabularyServiceContext(options))
        {
            arrange.Projects.Add(new Project
            {
                Id = projectId,
                UserId = userId,
                Title = "P",
                SourceLang = "en",
                TargetLang = "ru",
                FsrsSettings = new FsrsSettings(),
                Stats = new ProjectStats(),
                IsArchived = false,
                CreatedAt = now,
                UpdatedAt = now
            });
            arrange.Decks.Add(new Deck
            {
                Id = deckId,
                ProjectId = projectId,
                OwnerId = userId,
                Title = "Inbox",
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                CardCount = 0,
                IsPublic = false,
                CreatedAt = now,
                UpdatedAt = now
            });
            await arrange.SaveChangesAsync();

            var media = new Mock<IMediaService>();
            media
                .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var cardService = new CardService(
                arrange,
                new TermService(arrange, NullLogger<TermService>.Instance),
                media.Object,
                new NoteTypeService(arrange),
                Mock.Of<ILogger<CardService>>());

            var created = await cardService.CreateCardAsync(
                new CreateCardDto
                {
                    UserId = userId,
                    DeckId = deckId,
                    FieldValues = new Dictionary<string, NoteFieldValue>
                    {
                        [SentenceMiningNoteType.Expression] = new() { String = "test" },
                        [SentenceMiningNoteType.Word] = new() { String = "test" },
                        [SentenceMiningNoteType.Translation] = new() { String = "тест" },
                    },
                });
            cardId = created.Id;

            arrange.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId,
                State = 1,
                Step = 0,
                Stability = 0,
                Difficulty = 0,
                Due = now.AddMinutes(5),
                ElapsedDays = 0,
                ScheduledDays = 0,
                Reps = 1,
                Lapses = 0,
                IsSuspended = false,
                LastReview = now
            });
            await arrange.SaveChangesAsync();
        }

        await using var ctx = new TestVocabularyServiceContext(options);
        var userSettings = new Mock<IUserSettingsService>(MockBehavior.Strict);
        userSettings
            .Setup(s => s.GetUserSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSetting
            {
                UserId = userId,
                DailyGoalNew = 20,
                DailyGoalReview = 100,
                InterfaceLanguage = "en",
                UpdatedAt = now
            });

        var mediaMock = new Mock<IMediaService>(MockBehavior.Strict);
        mediaMock
            .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardSvc = new CardService(
            ctx,
            new TermService(ctx, NullLogger<TermService>.Instance),
            mediaMock.Object,
            new NoteTypeService(ctx),
            Mock.Of<ILogger<CardService>>());

        var fsrsMock = new Mock<IFsrsScheduler>();
        fsrsMock
            .Setup(f => f.GetNextStateAsync(
                It.IsAny<UserCardProgress>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<FsrsSettings?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCardProgress _, int _, DateTime reviewAt, int _, FsrsSettings? _, CancellationToken _) =>
                new FsrsNextState(1f, 5f, reviewAt.AddMinutes(10), 1, 1));

        var sut = StudyServiceTestFactory.Create(ctx, cardSvc, fsrsMock.Object, userSettings.Object, mediaMock.Object);

        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);
        session.QueueStats.Learning.Should().Be(1, "card is in learning state in the initial queue");

        var next = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);
        next.Should().NotBeNull("learn-ahead card must be seeded into the session queue");
        next!.Id.Should().Be(cardId);
    }

    [Fact]
    public async Task StartStudySession_Includes_UnreviewedProgressCard_With_StateZero_RepsZero()
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
        Guid cardId;

        await using (var arrange = new TestVocabularyServiceContext(options))
        {
            arrange.Projects.Add(new Project
            {
                Id = projectId,
                UserId = userId,
                Title = "P",
                SourceLang = "en",
                TargetLang = "ru",
                FsrsSettings = new FsrsSettings(),
                Stats = new ProjectStats(),
                IsArchived = false,
                CreatedAt = now,
                UpdatedAt = now
            });
            arrange.Decks.Add(new Deck
            {
                Id = deckId,
                ProjectId = projectId,
                OwnerId = userId,
                Title = "Inbox",
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                CardCount = 0,
                IsPublic = false,
                CreatedAt = now,
                UpdatedAt = now
            });
            await arrange.SaveChangesAsync();

            var media = new Mock<IMediaService>();
            media
                .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var cardService = new CardService(
                arrange,
                new TermService(arrange, NullLogger<TermService>.Instance),
                media.Object,
                new NoteTypeService(arrange),
                Mock.Of<ILogger<CardService>>());

            var created = await cardService.CreateCardAsync(
                new CreateCardDto
                {
                    UserId = userId,
                    DeckId = deckId,
                    FieldValues = new Dictionary<string, NoteFieldValue>
                    {
                        [SentenceMiningNoteType.Expression] = new() { String = "hello" },
                        [SentenceMiningNoteType.Word] = new() { String = "hello" },
                        [SentenceMiningNoteType.Translation] = new() { String = "привет" },
                    },
                });
            cardId = created.Id;

            // Simulates suspend/unsuspend or sync: progress exists but card was never reviewed.
            arrange.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId,
                State = 0,
                Step = 0,
                Stability = 0,
                Difficulty = 0,
                Due = now,
                ElapsedDays = 0,
                ScheduledDays = 0,
                Reps = 0,
                Lapses = 0,
                IsSuspended = false,
                LastReview = now
            });
            await arrange.SaveChangesAsync();
        }

        await using var ctx = new TestVocabularyServiceContext(options);
        var userSettings = new Mock<IUserSettingsService>(MockBehavior.Strict);
        userSettings
            .Setup(s => s.GetUserSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSetting
            {
                UserId = userId,
                DailyGoalNew = 20,
                DailyGoalReview = 100,
                InterfaceLanguage = "en",
                UpdatedAt = now
            });

        var mediaMock = new Mock<IMediaService>(MockBehavior.Strict);
        mediaMock
            .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardSvc = new CardService(
            ctx,
            new TermService(ctx, NullLogger<TermService>.Instance),
            mediaMock.Object,
            new NoteTypeService(ctx),
            Mock.Of<ILogger<CardService>>());

        var fsrsMock = new Mock<IFsrsScheduler>();
        fsrsMock
            .Setup(f => f.GetNextStateAsync(
                It.IsAny<UserCardProgress>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<FsrsSettings?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCardProgress _, int _, DateTime reviewAt, int _, FsrsSettings? _, CancellationToken _) =>
                new FsrsNextState(1f, 5f, reviewAt.AddMinutes(10), 1, 1));

        var sut = StudyServiceTestFactory.Create(ctx, cardSvc, fsrsMock.Object, userSettings.Object, mediaMock.Object);

        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);
        session.QueueStats.New.Should().Be(1, "unreviewed progress card must count as new in queue stats");

        var next = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);
        next.Should().NotBeNull("unreviewed progress card must be studyable");
        next!.Id.Should().Be(cardId);
    }

    [Fact]
    public async Task StartStudySession_Excludes_Suspended_UnreviewedProgressCard()
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
        Guid cardId;

        await using (var arrange = new TestVocabularyServiceContext(options))
        {
            arrange.Projects.Add(new Project
            {
                Id = projectId,
                UserId = userId,
                Title = "P",
                SourceLang = "en",
                TargetLang = "ru",
                FsrsSettings = new FsrsSettings(),
                Stats = new ProjectStats(),
                IsArchived = false,
                CreatedAt = now,
                UpdatedAt = now
            });
            arrange.Decks.Add(new Deck
            {
                Id = deckId,
                ProjectId = projectId,
                OwnerId = userId,
                Title = "Inbox",
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                CardCount = 0,
                IsPublic = false,
                CreatedAt = now,
                UpdatedAt = now
            });
            await arrange.SaveChangesAsync();

            var media = new Mock<IMediaService>();
            media
                .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var cardService = new CardService(
                arrange,
                new TermService(arrange, NullLogger<TermService>.Instance),
                media.Object,
                new NoteTypeService(arrange),
                Mock.Of<ILogger<CardService>>());

            var created = await cardService.CreateCardAsync(
                new CreateCardDto
                {
                    UserId = userId,
                    DeckId = deckId,
                    FieldValues = new Dictionary<string, NoteFieldValue>
                    {
                        [SentenceMiningNoteType.Expression] = new() { String = "hello" },
                        [SentenceMiningNoteType.Word] = new() { String = "hello" },
                        [SentenceMiningNoteType.Translation] = new() { String = "привет" },
                    },
                });
            cardId = created.Id;

            arrange.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId,
                State = 0,
                Step = 0,
                Stability = 0,
                Difficulty = 0,
                Due = now,
                ElapsedDays = 0,
                ScheduledDays = 0,
                Reps = 0,
                Lapses = 0,
                IsSuspended = true,
                LastReview = now
            });
            await arrange.SaveChangesAsync();
        }

        await using var ctx = new TestVocabularyServiceContext(options);
        var userSettings = new Mock<IUserSettingsService>(MockBehavior.Strict);
        userSettings
            .Setup(s => s.GetUserSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSetting
            {
                UserId = userId,
                DailyGoalNew = 20,
                DailyGoalReview = 100,
                InterfaceLanguage = "en",
                UpdatedAt = now
            });

        var mediaMock = new Mock<IMediaService>(MockBehavior.Strict);
        mediaMock
            .Setup(m => m.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardSvc = new CardService(
            ctx,
            new TermService(ctx, NullLogger<TermService>.Instance),
            mediaMock.Object,
            new NoteTypeService(ctx),
            Mock.Of<ILogger<CardService>>());

        var fsrsMock = new Mock<IFsrsScheduler>();
        var sut = StudyServiceTestFactory.Create(ctx, cardSvc, fsrsMock.Object, userSettings.Object, mediaMock.Object);

        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);
        session.QueueStats.New.Should().Be(0, "suspended unreviewed card must not be queued as new");

        var next = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);
        next.Should().BeNull("suspended card must not be studyable");
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
