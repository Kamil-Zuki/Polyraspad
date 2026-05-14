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
/// P3: GetNextCard must expose study content derived from note.field_values (not parallel legacy columns).
/// </summary>
public class StudyServiceGetNextCardNoteDerivedContentTests
{
    [Fact]
    public async Task GetNextCard_Should_Map_Content_And_Source_From_Note_Fields_TermFirst_Surface_Word()
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
        const string expression = "They slept well that night.";
        const string surfaceWord = "slept";
        const string translation = "Они хорошо выспались.";
        const string sourceTitle = "Reader";
        const string sourceUrl = "https://example.com/page";

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
                        [SentenceMiningNoteType.Expression] = new() { String = expression },
                        [SentenceMiningNoteType.Word] = new() { String = surfaceWord },
                        [SentenceMiningNoteType.Translation] = new() { String = translation },
                        [SentenceMiningNoteType.SourceTitle] = new() { String = sourceTitle },
                        [SentenceMiningNoteType.SourceUrl] = new() { String = sourceUrl },
                    },
                });
            cardId = created.Id;

            arrangeContext.UserCardProgresses.Add(new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = projectId,
                State = 1, // LEARNING — попадает в очередь сессии как в StudyServiceLearningCardsInQueueTests
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
        var next = await sut.GetNextCardAsync(session.Id, userId, CancellationToken.None);

        next.Should().NotBeNull();
        next!.SrsState.State.Should().Be("LEARNING");
        next!.Content.Sentence.Should().Be(expression);
        next.Content.Translation.Should().Be(translation);
        next.Content.TargetLemma.Should().Be(surfaceWord, "term-first: mined surface form from Word field, not lemma text");
        next.SourceMeta.Should().NotBeNull();
        next.SourceMeta!.Title.Should().Be(sourceTitle);
        next.SourceMeta.Url.Should().Be(sourceUrl);
        next.Content.TargetIndex.Len.Should().BeGreaterThan(0);
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
