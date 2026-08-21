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

/// <summary>
/// Anki parity: learning cards due in the future must not appear before their step,
/// mixed with new/review cards in the same session.
/// </summary>
public class StudyServiceLearningDeferTests
{
    [Fact]
    public async Task GetNextCardAsync_DefersFutureLearningCard_UntilOtherCardsAreShown()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        Guid newCardId;
        Guid learningCardId;
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
                FsrsSettings = new FsrsSettings { LearningStepsSeconds = [60, 600] },
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

            newCardId = (await cardSeed.CreateCardAsync(new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new() { String = "alpha" },
                    [SentenceMiningNoteType.Word] = new() { String = "alpha" },
                    [SentenceMiningNoteType.Translation] = new() { String = "a" },
                },
            })).Id;

            learningCardId = (await cardSeed.CreateCardAsync(new CreateCardDto
            {
                UserId = userId,
                DeckId = deckId,
                FieldValues = new Dictionary<string, NoteFieldValue>
                {
                    [SentenceMiningNoteType.Expression] = new() { String = "beta" },
                    [SentenceMiningNoteType.Word] = new() { String = "beta" },
                    [SentenceMiningNoteType.Translation] = new() { String = "b" },
                },
            })).Id;

            arrangeContext.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = learningCardId,
                ProjectId = projectId,
                State = 1,
                Step = 1,
                Stability = 1f,
                Difficulty = 5f,
                Due = now.AddMinutes(10),
                LastReview = now,
                Reps = 1,
                Lapses = 0,
                IsSuspended = false,
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
            .Setup(f => f.GetNextStateAsync(
                It.IsAny<UserCardProgress>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<FsrsSettings?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCardProgress p, int rating, DateTime reviewAt, int _, FsrsSettings? _, CancellationToken _) =>
                new FsrsNextState(
                    Stability: 1f,
                    Difficulty: 5f,
                    Due: reviewAt.AddMinutes(rating == 3 ? 10 : 1),
                    State: 1,
                    Step: 1));

        var sut = StudyServiceTestFactory.Create(
            actContext,
            cardService,
            fsrsMock.Object,
            userSettingsMock.Object,
            mediaServiceMock.Object);

        var session = await sut.StartStudySessionAsync(userId, projectId, deckId, CancellationToken.None);
        var first = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);

        first.Should().NotBeNull();
        first!.Id.Should().Be(newCardId, "learning due через 10m не должна обгонять новую карту в очереди");
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
