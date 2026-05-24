using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Diagnostics;

using Microsoft.Extensions.Logging;

using Moq;

using VocabularyService.Data;

using VocabularyService.Data.Entities;

using VocabularyService.Data.Entities.JsonTypes;

using VocabularyService.Services;

using Xunit;



namespace VocabularyService.Tests;



/// <summary>

/// Regression tests for vocabulary stats (SR-ANL-01). Counts merge Reader/Vocabulary

/// UserTermStatus with FSRS card progress per ProjectTerm.

/// </summary>

public class AnalyticsServiceVocabularyStatsTests

{

    [Fact]

    public async Task GetVocabularyStatsAsync_CountsKnownStatusesAndMatureFsrsCardsAsKnownTerms()

    {

        var dbName = Guid.NewGuid().ToString("N");

        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()

            .UseInMemoryDatabase(dbName)

            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))

            .Options;



        var userId = Guid.NewGuid();

        var projectId = Guid.NewGuid();

        var deckId = Guid.NewGuid();

        var matureCardId = Guid.NewGuid();

        var reviewingCardId = Guid.NewGuid();

        var matureNoteId = Guid.NewGuid();

        var reviewingNoteId = Guid.NewGuid();

        var matureTermId = Guid.NewGuid();

        var reviewingTermId = Guid.NewGuid();

        var now = DateTime.UtcNow;



        await using (var arrangeContext = new TestVocabularyServiceContext(options))

        {

            arrangeContext.Projects.Add(new Project

            {

                Id = projectId,

                UserId = userId,

                Title = "English",

                SourceLang = "en",

                TargetLang = "ru",

                FsrsSettings = new FsrsSettings(),

                Stats = new ProjectStats(),

                IsArchived = false,

                CreatedAt = now,

                UpdatedAt = now

            });



            var termIds = new[]

            {

                (Id: Guid.NewGuid(), Text: "certainly", Status: "KNOWN"),

                (Id: Guid.NewGuid(), Text: "recognize", Status: "SAVED"),

                (Id: Guid.NewGuid(), Text: "hello", Status: "LINGQ"),

                (Id: Guid.NewGuid(), Text: "fresh", Status: "NEW"),

                (Id: Guid.NewGuid(), Text: "noise", Status: "IGNORED"),

                (Id: matureTermId, Text: "mature-card-only", Status: "NEW"),

                (Id: reviewingTermId, Text: "review-card-only", Status: "SAVED"),

            };



            foreach (var term in termIds)

            {

                arrangeContext.ProjectTerms.Add(new ProjectTerm

                {

                    Id = term.Id,

                    ProjectId = projectId,

                    Text = term.Text,

                    NormalizedText = term.Text.ToLowerInvariant(),

                    Type = "WORD",

                    Language = "en",

                    CreatedAt = now,

                    UpdatedAt = now

                });



                arrangeContext.UserTermStatuses.Add(new UserTermStatus

                {

                    Id = Guid.NewGuid(),

                    UserId = userId,

                    ProjectId = projectId,

                    ProjectTermId = term.Id,

                    Status = term.Status,

                    CreatedAt = now,

                    UpdatedAt = now

                });

            }



            arrangeContext.Decks.Add(new Deck

            {

                Id = deckId,

                ProjectId = projectId,

                OwnerId = userId,

                Title = "Deck",

                Description = null,

                CoverImageUrl = null,

                IsPublic = false,

                ContributionPolicy = "OPEN",

                LicenseType = "PRIVATE",

                ForkedFromId = null,

                CardCount = 2,

                CreatedAt = now,

                UpdatedAt = now

            });



            arrangeContext.Notes.AddRange(

                new Note

                {

                    Id = matureNoteId,

                    DeckId = deckId,

                    CreatorId = userId,

                    NoteTypeId = Guid.NewGuid(),

                    FieldValues = new Dictionary<string, NoteFieldValue>(),

                    CreatedAt = now,

                    UpdatedAt = now

                },

                new Note

                {

                    Id = reviewingNoteId,

                    DeckId = deckId,

                    CreatorId = userId,

                    NoteTypeId = Guid.NewGuid(),

                    FieldValues = new Dictionary<string, NoteFieldValue>(),

                    CreatedAt = now,

                    UpdatedAt = now

                });



            arrangeContext.Cards.AddRange(

                new Card

                {

                    Id = matureCardId,

                    DeckId = deckId,

                    CreatorId = userId,

                    NoteId = matureNoteId,

                    SearchDocument = "mature-card-only",

                    ProjectTermId = matureTermId,

                    CreatedAt = now,

                    UpdatedAt = now

                },

                new Card

                {

                    Id = reviewingCardId,

                    DeckId = deckId,

                    CreatorId = userId,

                    NoteId = reviewingNoteId,

                    SearchDocument = "review-card-only",

                    ProjectTermId = reviewingTermId,

                    CreatedAt = now,

                    UpdatedAt = now

                });



            arrangeContext.UserCardProgresses.AddRange(

                new UserCardProgress

                {

                    Id = Guid.NewGuid(),

                    UserId = userId,

                    CardId = matureCardId,

                    ProjectId = projectId,

                    State = 2,

                    Step = 0,

                    Stability = 30,

                    Difficulty = 5,

                    Due = now.AddDays(30),

                    ElapsedDays = 21,

                    ScheduledDays = 21,

                    Reps = 5,

                    Lapses = 0,

                    IsSuspended = false,

                    LastReview = now

                },

                new UserCardProgress

                {

                    Id = Guid.NewGuid(),

                    UserId = userId,

                    CardId = reviewingCardId,

                    ProjectId = projectId,

                    State = 1,

                    Step = 1,

                    Stability = 1,

                    Difficulty = 5,

                    Due = now.AddMinutes(10),

                    ElapsedDays = 0,

                    ScheduledDays = 0,

                    Reps = 1,

                    Lapses = 0,

                    IsSuspended = false,

                    LastReview = now

                });



            await arrangeContext.SaveChangesAsync();

        }



        await using (var actContext = new TestVocabularyServiceContext(options))

        {

            var logger = new Mock<ILogger<AnalyticsService>>();

            var userSettings = new UserSettingsService(actContext, new Mock<ILogger<UserSettingsService>>().Object);

            var sut = new AnalyticsService(actContext, logger.Object, userSettings);



            var result = await sut.GetVocabularyStatsAsync(userId, projectId, CancellationToken.None);



            result.Should().NotBeNull();

            result.ProjectId.Should().Be(projectId);

            result.TotalLemmas.Should().Be(6, "IGNORED is excluded from progress totals");

            result.MatureCount.Should().Be(2, "explicit KNOWN + mature FSRS card");

            result.SavedCount.Should().Be(2, "saved-only terms without card-backed review");

            result.ReviewingCount.Should().Be(1, "non-mature card-backed term");

            result.LearningCount.Should().Be(3, "saved + reviewing");

            result.NewCount.Should().Be(1, "NEW without stronger card/status signal");

            result.CefrLevel.Should().NotBeNull();

            result.EstimatedFluency.Should().BeGreaterThanOrEqualTo(0);

        }

    }



    [Fact]

    public async Task GetVocabularyStatsAsync_OverdueMatureReviewCard_StillCountsAsKnownTerm()

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

        var noteId = Guid.NewGuid();

        var termId = Guid.NewGuid();

        var now = DateTime.UtcNow;



        await using (var arrangeContext = new TestVocabularyServiceContext(options))

        {

            arrangeContext.Projects.Add(new Project

            {

                Id = projectId,

                UserId = userId,

                Title = "English",

                SourceLang = "en",

                TargetLang = "ru",

                FsrsSettings = new FsrsSettings(),

                Stats = new ProjectStats(),

                IsArchived = false,

                CreatedAt = now,

                UpdatedAt = now

            });



            arrangeContext.ProjectTerms.Add(new ProjectTerm

            {

                Id = termId,

                ProjectId = projectId,

                Text = "overdue-mature",

                NormalizedText = "overdue-mature",

                Type = "WORD",

                Language = "en",

                CreatedAt = now,

                UpdatedAt = now

            });



            arrangeContext.UserTermStatuses.Add(new UserTermStatus

            {

                Id = Guid.NewGuid(),

                UserId = userId,

                ProjectId = projectId,

                ProjectTermId = termId,

                Status = "NEW",

                CreatedAt = now,

                UpdatedAt = now

            });



            arrangeContext.Decks.Add(new Deck

            {

                Id = deckId,

                ProjectId = projectId,

                OwnerId = userId,

                Title = "Deck",

                Description = null,

                CoverImageUrl = null,

                IsPublic = false,

                ContributionPolicy = "OPEN",

                LicenseType = "PRIVATE",

                ForkedFromId = null,

                CardCount = 1,

                CreatedAt = now,

                UpdatedAt = now

            });



            arrangeContext.Notes.Add(new Note

            {

                Id = noteId,

                DeckId = deckId,

                CreatorId = userId,

                NoteTypeId = Guid.NewGuid(),

                FieldValues = new Dictionary<string, NoteFieldValue>(),

                CreatedAt = now,

                UpdatedAt = now

            });



            arrangeContext.Cards.Add(new Card

            {

                Id = cardId,

                DeckId = deckId,

                CreatorId = userId,

                NoteId = noteId,

                SearchDocument = "overdue-mature",

                ProjectTermId = termId,

                CreatedAt = now,

                UpdatedAt = now

            });



            arrangeContext.UserCardProgresses.Add(new UserCardProgress

            {

                Id = Guid.NewGuid(),

                UserId = userId,

                CardId = cardId,

                ProjectId = projectId,

                State = 2,

                Step = 0,

                Stability = 30,

                Difficulty = 5,

                Due = now.AddDays(-1),

                ElapsedDays = 22,

                ScheduledDays = 21,

                Reps = 6,

                Lapses = 0,

                IsSuspended = false,

                LastReview = now.AddDays(-22)

            });



            await arrangeContext.SaveChangesAsync();

        }



        await using (var actContext = new TestVocabularyServiceContext(options))

        {

            var logger = new Mock<ILogger<AnalyticsService>>();

            var userSettings = new UserSettingsService(actContext, new Mock<ILogger<UserSettingsService>>().Object);

            var sut = new AnalyticsService(actContext, logger.Object, userSettings);



            var result = await sut.GetVocabularyStatsAsync(userId, projectId, CancellationToken.None);



            result.MatureCount.Should().Be(1, "mature review card with ScheduledDays >= 21 counts as known even when due");

            result.ReviewingCount.Should().Be(0);

            result.NewCount.Should().Be(0);

        }

    }



    [Fact]

    public async Task GetVocabularyStatsAsync_WithThreeKnownTerms_ReportsA1ProgressAndWordsToNextLevel()

    {

        var dbName = Guid.NewGuid().ToString("N");

        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()

            .UseInMemoryDatabase(dbName)

            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))

            .Options;



        var userId = Guid.NewGuid();

        var projectId = Guid.NewGuid();

        var now = DateTime.UtcNow;



        await using (var arrangeContext = new TestVocabularyServiceContext(options))

        {

            arrangeContext.Projects.Add(new Project

            {

                Id = projectId,

                UserId = userId,

                Title = "English",

                SourceLang = "en",

                TargetLang = "ru",

                FsrsSettings = new FsrsSettings(),

                Stats = new ProjectStats(),

                IsArchived = false,

                CreatedAt = now,

                UpdatedAt = now

            });



            for (var i = 0; i < 3; i++)

            {

                var termId = Guid.NewGuid();

                arrangeContext.ProjectTerms.Add(new ProjectTerm

                {

                    Id = termId,

                    ProjectId = projectId,

                    Text = $"known-{i}",

                    NormalizedText = $"known-{i}",

                    Type = "WORD",

                    Language = "en",

                    CreatedAt = now,

                    UpdatedAt = now

                });



                arrangeContext.UserTermStatuses.Add(new UserTermStatus

                {

                    Id = Guid.NewGuid(),

                    UserId = userId,

                    ProjectId = projectId,

                    ProjectTermId = termId,

                    Status = "KNOWN",

                    CreatedAt = now,

                    UpdatedAt = now

                });

            }



            await arrangeContext.SaveChangesAsync();

        }



        await using (var actContext = new TestVocabularyServiceContext(options))

        {

            var logger = new Mock<ILogger<AnalyticsService>>();

            var userSettings = new UserSettingsService(actContext, new Mock<ILogger<UserSettingsService>>().Object);

            var sut = new AnalyticsService(actContext, logger.Object, userSettings);



            var result = await sut.GetVocabularyStatsAsync(userId, projectId, CancellationToken.None);



            result.MatureCount.Should().Be(3);

            result.CefrLevel.Code.Should().Be("A1");

            result.CefrLevel.ProgressPercent.Should().Be(0);

            result.CefrLevel.WordsToNextLevel.Should().Be(497);

        }

    }



    [Fact]

    public async Task GetVocabularyStatsAsync_AtA2Threshold_ReportsNextLevelProgress()

    {

        var dbName = Guid.NewGuid().ToString("N");

        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()

            .UseInMemoryDatabase(dbName)

            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))

            .Options;



        var userId = Guid.NewGuid();

        var projectId = Guid.NewGuid();

        var now = DateTime.UtcNow;

        const int knownCount = 500;



        await using (var arrangeContext = new TestVocabularyServiceContext(options))

        {

            arrangeContext.Projects.Add(new Project

            {

                Id = projectId,

                UserId = userId,

                Title = "English",

                SourceLang = "en",

                TargetLang = "ru",

                FsrsSettings = new FsrsSettings(),

                Stats = new ProjectStats(),

                IsArchived = false,

                CreatedAt = now,

                UpdatedAt = now

            });



            for (var i = 0; i < knownCount; i++)

            {

                var termId = Guid.NewGuid();

                arrangeContext.ProjectTerms.Add(new ProjectTerm

                {

                    Id = termId,

                    ProjectId = projectId,

                    Text = $"known-{i}",

                    NormalizedText = $"known-{i}",

                    Type = "WORD",

                    Language = "en",

                    CreatedAt = now,

                    UpdatedAt = now

                });



                arrangeContext.UserTermStatuses.Add(new UserTermStatus

                {

                    Id = Guid.NewGuid(),

                    UserId = userId,

                    ProjectId = projectId,

                    ProjectTermId = termId,

                    Status = "KNOWN",

                    CreatedAt = now,

                    UpdatedAt = now

                });

            }



            await arrangeContext.SaveChangesAsync();

        }



        await using (var actContext = new TestVocabularyServiceContext(options))

        {

            var logger = new Mock<ILogger<AnalyticsService>>();

            var userSettings = new UserSettingsService(actContext, new Mock<ILogger<UserSettingsService>>().Object);

            var sut = new AnalyticsService(actContext, logger.Object, userSettings);



            var result = await sut.GetVocabularyStatsAsync(userId, projectId, CancellationToken.None);



            result.MatureCount.Should().Be(knownCount);

            result.CefrLevel.Code.Should().Be("A2");

            result.CefrLevel.ProgressPercent.Should().Be(0);

            result.CefrLevel.WordsToNextLevel.Should().Be(700);

        }

    }



    private sealed class TestVocabularyServiceContext : VocabularyServiceContext

    {

        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options) : base(options) { }



        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Card>().Ignore(c => c.SearchVector);

        }

    }

}

