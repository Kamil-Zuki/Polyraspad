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
/// Регрессия: прогресс карточки должен браться по ProjectId активной сессии, иначе при дубликатах
/// user_card_progress (один user + card, разные проекты) FSRS видит неверный Step и залипает на одном learning-интервале.
/// </summary>
public class StudyServiceProjectScopedProgressTests
{
    [Fact]
    public async Task GetNextCardAsync_PassesFsrsProgress_FromSessionProject_WhenDuplicateRowsExist()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        Guid cardId;
        var now = DateTime.UtcNow;

        await using (var arrangeContext = new TestVocabularyServiceContext(options))
        {
            foreach (var pid in new[] { projectId1, projectId2 })
            {
                arrangeContext.Projects.Add(new Project
                {
                    Id = pid,
                    UserId = userId,
                    Title = pid == projectId1 ? "P1" : "P2",
                    SourceLang = "en",
                    TargetLang = "ru",
                    FsrsSettings = new FsrsSettings(),
                    Stats = new ProjectStats(),
                    IsArchived = false,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            arrangeContext.Decks.Add(new Deck
            {
                Id = deckId,
                ProjectId = projectId1,
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
                        [SentenceMiningNoteType.Expression] = new() { String = "w" },
                        [SentenceMiningNoteType.Word] = new() { String = "w" },
                        [SentenceMiningNoteType.Translation] = new() { String = "t" },
                    },
                });
            cardId = created.Id;

            // «Плохая» строка из другого проекта (воспроизводит исторический/сбойный дубликат).
            arrangeContext.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId2,
                State = 1,
                Step = 0,
                Stability = 1f,
                Difficulty = 5f,
                Due = now,
                LastReview = now,
                Reps = 1,
                Lapses = 0,
                IsSuspended = false,
            });

            arrangeContext.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId1,
                State = 1,
                Step = 1,
                Stability = 2f,
                Difficulty = 5f,
                Due = now,
                LastReview = now,
                Reps = 2,
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

        var lastStepPassedToFsrs = -99;
        var fsrsMock = new Mock<IFsrsScheduler>();
        fsrsMock
            .Setup(f => f.GetNextStateAsync(
                It.IsAny<UserCardProgress>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<FsrsSettings?>(),
                It.IsAny<CancellationToken>()))
            .Callback<UserCardProgress, int, DateTime, int, FsrsSettings?, CancellationToken>(
                (progress, _, _, _, _, _) => lastStepPassedToFsrs = progress.Step)
            .ReturnsAsync(new FsrsNextState(
                Stability: 1f,
                Difficulty: 5f,
                Due: DateTime.UtcNow.AddMinutes(10),
                State: 1,
                Step: 1));

        var sut = StudyServiceTestFactory.Create(
            actContext,
            cardService,
            fsrsMock.Object,
            userSettingsMock.Object,
            mediaServiceMock.Object);

        var session = await sut.StartStudySessionAsync(userId, projectId1, deckId, CancellationToken.None);
        await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);

        lastStepPassedToFsrs.Should().Be(1, "FSRS должен видеть шаг прогресса текущего проекта сессии, а не чужого дубликата");
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
