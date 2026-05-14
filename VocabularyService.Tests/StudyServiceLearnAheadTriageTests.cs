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
using VocabularyService.Domain;
using VocabularyService.Dtos.Cards;
using VocabularyService.Dtos.Study;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

/// <summary>
/// Тесты для проверки логики Learn Ahead (обучение заранее).
/// Проверяем, что карточки в состоянии LEARNING, которые скоро станут доступными,
/// включаются в сессию обучения.
/// </summary>
public class StudyServiceLearnAheadTriageTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task StartStudySessionAsync_Should_IncludeLearningOrRelearningCard_When_DueIn5Minutes(short state)
    {
        // Arrange
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
                CardCount = 0,
                IsPublic = false,
                CreatedAt = now,
                UpdatedAt = now
            });

            await arrangeContext.SaveChangesAsync();

            var mediaArrange = new Mock<IMediaService>();
            mediaArrange
                .Setup(s => s.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var cardSeed = new CardService(
                arrangeContext,
                new TermService(arrangeContext, NullLogger<TermService>.Instance),
                mediaArrange.Object,
                new NoteTypeService(arrangeContext),
                Mock.Of<ILogger<CardService>>());

            var created = await cardSeed.CreateCardAsync(
                new CreateCardDto
                {
                    UserId = userId,
                    DeckId = deckId,
                    FieldValues = new Dictionary<string, NoteFieldValue>
                    {
                        [SentenceMiningNoteType.Expression] = new() { String = "Hello world" },
                        [SentenceMiningNoteType.Word] = new() { String = "Hello" },
                        [SentenceMiningNoteType.Translation] = new() { String = "Привет мир" },
                    },
                });
            cardId = created.Id;

            // Карточка в состоянии LEARNING, due через 5 минут
            // По умолчанию LearnAheadLimitMinutes обычно 20 минут,
            // поэтому карточка должна попасть в очередь.
            arrangeContext.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId,
                State = state,
                Step = 0,
                Stability = 0,
                Difficulty = 0,
                Due = now.AddMinutes(5), // Через 5 минут
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

        var mediaServiceMock = new Mock<IMediaService>(MockBehavior.Strict);
        mediaServiceMock
            .Setup(s => s.FillCardMediaUrlsAsync(It.IsAny<CardMedia?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cardService = new CardService(
            actContext,
            new TermService(actContext, NullLogger<TermService>.Instance),
            mediaServiceMock.Object,
            new NoteTypeService(actContext),
            Mock.Of<ILogger<CardService>>());

        var sut = new StudyService(
            actContext,
            Mock.Of<ILogger<StudyService>>(),
            cardService,
            Mock.Of<IDeckService>(),
            userSettingsMock.Object,
            Mock.Of<IFsrsScheduler>(),
            Mock.Of<IAnswerValidationService>(),
            mediaServiceMock.Object,
            RedisTestHelper.CreateConnectionMultiplexer());

        // Act
        // Запуск сессии по колоде с одной LEARNING-карточкой, которая будет доступна через 5 минут
        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);

        // Assert
        session.Should().NotBeNull();
        session.QueueStats.Learning.Should().Be(1, "карточка LEARNING, доступная через 5 минут, должна быть включена в сессию (Learn Ahead)");
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

