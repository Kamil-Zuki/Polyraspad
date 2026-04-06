using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Dtos.Study;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

/// <summary>
/// Регрессионный тест: сессия обучения должна возвращать карточки в состоянии LEARNING,
/// иначе при "1 LEARNING" на дашборде GetNextCard сразу возвращает null (Session complete).
/// </summary>
public class StudyServiceLearningCardsInQueueTests
{
    [Fact]
    public async Task GetNextCard_Should_ReturnCard_When_DeckHasLearningCardDueNow()
    {
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
                Title = "P",
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
                Title = "Deck",
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
                Sentence = "Hello world",
                Translation = "Привет мир",
                TargetWord = "Hello",
                TargetIndex = new TargetIndex { Start = 0, Len = 5 },
                CreatedAt = now,
                UpdatedAt = now
            });

            // Карточка в состоянии LEARNING, due сейчас — должна попасть в очередь сессии
            arrangeContext.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId,
                State = 1, // LEARNING
                Step = 0,
                Stability = 0,
                Difficulty = 0,
                Due = now,
                ElapsedDays = 0,
                ScheduledDays = 0,
                Reps = 1,
                Lapses = 0,
                IsSuspended = false,
                LastReview = now
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

        var mediaStorageMock = new Mock<IMediaStorageService>(MockBehavior.Strict);
        mediaStorageMock
            .Setup(s => s.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardService = new CardService(
            actContext,
            mediaStorageMock.Object,
            Mock.Of<ILogger<CardService>>());

        var sut = new StudyService(
            actContext,
            Mock.Of<ILogger<StudyService>>(),
            cardService,
            Mock.Of<IDeckService>(),
            userSettingsMock.Object,
            Mock.Of<IFsrsScheduler>(),
            Mock.Of<IAnswerValidationService>(),
            mediaStorageMock.Object,
            RedisTestHelper.CreateConnectionMultiplexer());

        // Запуск сессии по колоде с одной LEARNING-карточкой
        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);
        session.Should().NotBeNull();
        session.Id.Should().NotBeEmpty();
        session.QueueStats.Learning.Should().Be(1, "в очереди должна быть одна LEARNING-карточка");

        // Следующая карточка должна быть возвращена, а не "сессия завершена"
        var nextCard = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);
        nextCard.Should().NotBeNull("при наличии LEARNING-карточки GetNextCard не должен возвращать null");
        nextCard!.Id.Should().Be(cardId);
        nextCard.SrsState.State.Should().Be("LEARNING");
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
