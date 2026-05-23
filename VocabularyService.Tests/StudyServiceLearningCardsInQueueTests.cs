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
/// Регрессионный тест: сессия обучения должна возвращать карточки в состоянии LEARNING,
/// иначе при "1 LEARNING" на дашборде GetNextCard сразу возвращает null (Session complete).
/// </summary>
public class StudyServiceLearningCardsInQueueTests
{
    [Theory]
    [InlineData(1, "LEARNING")]
    [InlineData(3, "RELEARNING")]
    public async Task GetNextCard_Should_ReturnCard_When_DeckHasLearningOrRelearningCardDueNow(short state, string expectedState)
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

            // Карточка в состоянии LEARNING, due сейчас — должна попасть в очередь сессии
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

        var fsrsMock = new Mock<IFsrsScheduler>();
        fsrsMock
            .Setup(f => f.GetNextStateAsync(It.IsAny<UserCardProgress>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<FsrsSettings?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCardProgress progress, int _, DateTime reviewAt, int _, FsrsSettings? _, CancellationToken _) =>
                new FsrsNextState(
                    Stability: progress.Stability,
                    Difficulty: progress.Difficulty,
                    Due: reviewAt.AddMinutes(10),
                    State: progress.State,
                    Step: progress.Step));

        var sut = StudyServiceTestFactory.Create(
            actContext,
            cardService,
            fsrsMock.Object,
            userSettingsMock.Object,
            mediaServiceMock.Object);

        // Запуск сессии по колоде с одной LEARNING-карточкой
        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);
        session.Should().NotBeNull();
        session.Id.Should().NotBeEmpty();
        session.QueueStats.Learning.Should().Be(1, "в очереди должна быть одна LEARNING/RELEARNING-карточка");

        // Следующая карточка должна быть возвращена, а не "сессия завершена"
        var nextCard = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);
        nextCard.Should().NotBeNull("при наличии LEARNING-карточки GetNextCard не должен возвращать null");
        nextCard!.Id.Should().Be(cardId);
        nextCard.SrsState.State.Should().Be(expectedState);
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

