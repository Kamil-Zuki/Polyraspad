using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class StudyServiceLearnAheadTests
{
    [Fact]
    public async Task GetNextCardAsync_ReturnsLearningCard_WithinLearnAheadLimit_AfterReview()
    {
        // 1. Arrange
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var arrangeContext = new TestVocabularyServiceContext(options))
        {
            arrangeContext.Projects.Add(new Project
            {
                Id = projectId,
                UserId = userId,
                Title = "Test Project",
                SourceLang = "en",
                TargetLang = "ru",
                FsrsSettings = new FsrsSettings(),
                Stats = new ProjectStats(),
                IsArchived = false,
                CreatedAt = now,
                UpdatedAt = now
            });

            arrangeContext.Decks.Add(new Deck
            {
                Id = deckId,
                ProjectId = projectId,
                OwnerId = userId,
                Title = "Test Deck",
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                CardCount = 1,
                IsPublic = false,
                CreatedAt = now,
                UpdatedAt = now
            });

            arrangeContext.Cards.Add(new Card
            {
                Id = cardId,
                DeckId = deckId,
                CreatorId = userId,
                Sentence = "Test sentence",
                Translation = "Test translation",
                TargetWord = "Test",
                TargetIndex = new TargetIndex { Start = 0, Len = 4 },
                CreatedAt = now,
                UpdatedAt = now
            });

            await arrangeContext.SaveChangesAsync();
        }

        await using var actContext = new TestVocabularyServiceContext(options);
        var userSettingsMock = new Mock<IUserSettingsService>(MockBehavior.Strict);
        userSettingsMock
            .Setup(s => s.GetUserSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSetting
            {
                UserId = userId,
                DailyGoalNew = 20,
                DailyGoalReview = 100,
                InterfaceLanguage = "en",
                UpdatedAt = now
            });

        var mediaServiceMock = new Mock<IMediaService>(MockBehavior.Strict);
        mediaServiceMock
            .Setup(s => s.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardService = new CardService(
            actContext,
            new LemmaService(actContext, NullLogger<LemmaService>.Instance),
            new TermService(actContext, NullLogger<TermService>.Instance),
            mediaServiceMock.Object,
            Mock.Of<ILogger<CardService>>());

        var fsrsMock = new Mock<IFsrsScheduler>();
        fsrsMock
            .Setup(f => f.GetNextStateAsync(It.IsAny<UserCardProgress>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<FsrsSettings?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FsrsNextState(
                Stability: 1.0f,
                Difficulty: 5.0f,
                Due: DateTime.UtcNow.AddMinutes(10), // Within 20 min Learn Ahead Limit
                State: 1, // LEARNING
                Step: 1
            ));

        var sut = new StudyService(
            actContext,
            Mock.Of<ILogger<StudyService>>(),
            cardService,
            Mock.Of<IDeckService>(),
            userSettingsMock.Object,
            fsrsMock.Object,
            Mock.Of<IAnswerValidationService>(),
            mediaServiceMock.Object,
            RedisTestHelper.CreateConnectionMultiplexer());

        // 2. Act
        // Start session
        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);

        // Call GetNextCardAsync -> should return the NEW card
        var firstCard = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);
        firstCard.Should().NotBeNull("the deck has a new card");
        firstCard!.Id.Should().Be(cardId);

        // Submit review -> Good (rating 3)
        await sut.SubmitReviewAsync(session.Id, userId, cardId, 3, 5000, null, CancellationToken.None);

        // Call GetNextCardAsync -> should return the card again because it is due in 10 minutes (within 20m learn-ahead limit)
        var nextCard = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);

        // 3. Assert
        nextCard.Should().NotBeNull("the card should be shown again via Learn Ahead limit");
        nextCard!.Id.Should().Be(cardId);
    }

    [Fact]
    public async Task GetNextCardAsync_DoesNotBurySameLearningCard_WhenTermWasAlreadySeen()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var arrangeContext = new TestVocabularyServiceContext(options))
        {
            arrangeContext.Projects.Add(new Project
            {
                Id = projectId,
                UserId = userId,
                Title = "Test Project",
                SourceLang = "en",
                TargetLang = "ru",
                FsrsSettings = new FsrsSettings(),
                Stats = new ProjectStats(),
                IsArchived = false,
                CreatedAt = now,
                UpdatedAt = now
            });

            arrangeContext.Decks.Add(new Deck
            {
                Id = deckId,
                ProjectId = projectId,
                OwnerId = userId,
                Title = "Test Deck",
                ContributionPolicy = "OPEN",
                LicenseType = "PRIVATE",
                CardCount = 1,
                IsPublic = false,
                CreatedAt = now,
                UpdatedAt = now
            });

            arrangeContext.ProjectTerms.Add(new ProjectTerm
            {
                Id = termId,
                ProjectId = projectId,
                Text = "test",
                NormalizedText = "test",
                Type = "WORD",
                Language = "en",
                CreatedAt = now,
                UpdatedAt = now
            });

            arrangeContext.Cards.Add(new Card
            {
                Id = cardId,
                DeckId = deckId,
                CreatorId = userId,
                ProjectTermId = termId,
                Sentence = "Test sentence",
                Translation = "Test translation",
                TargetWord = "Test",
                TargetIndex = new TargetIndex { Start = 0, Len = 4 },
                CreatedAt = now,
                UpdatedAt = now
            });

            await arrangeContext.SaveChangesAsync();
        }

        await using var actContext = new TestVocabularyServiceContext(options);
        var userSettingsMock = new Mock<IUserSettingsService>(MockBehavior.Strict);
        userSettingsMock
            .Setup(s => s.GetUserSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSetting
            {
                UserId = userId,
                DailyGoalNew = 20,
                DailyGoalReview = 100,
                InterfaceLanguage = "en",
                UpdatedAt = now
            });

        var mediaServiceMock = new Mock<IMediaService>(MockBehavior.Strict);
        mediaServiceMock
            .Setup(s => s.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardService = new CardService(
            actContext,
            new LemmaService(actContext, NullLogger<LemmaService>.Instance),
            new TermService(actContext, NullLogger<TermService>.Instance),
            mediaServiceMock.Object,
            Mock.Of<ILogger<CardService>>());

        var fsrsMock = new Mock<IFsrsScheduler>();
        fsrsMock
            .Setup(f => f.GetNextStateAsync(It.IsAny<UserCardProgress>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<FsrsSettings?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FsrsNextState(
                Stability: 1.0f,
                Difficulty: 5.0f,
                Due: DateTime.UtcNow.AddMinutes(10),
                State: 1,
                Step: 1
            ));

        var sut = new StudyService(
            actContext,
            Mock.Of<ILogger<StudyService>>(),
            cardService,
            Mock.Of<IDeckService>(),
            userSettingsMock.Object,
            fsrsMock.Object,
            Mock.Of<IAnswerValidationService>(),
            mediaServiceMock.Object,
            RedisTestHelper.CreateConnectionMultiplexer());

        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);
        var firstCard = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);
        firstCard.Should().NotBeNull();
        firstCard!.Id.Should().Be(cardId);

        await sut.SubmitReviewAsync(session.Id, userId, cardId, 3, 5000, null, CancellationToken.None);

        var learningRepeat = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);

        learningRepeat.Should().NotBeNull("same-card learning repeats are not sibling cards and should not be buried");
        learningRepeat!.Id.Should().Be(cardId);
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

