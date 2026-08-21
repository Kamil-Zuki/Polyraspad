using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Dtos.Analytics;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

/// <summary>
/// Regression tests for heatmap (SR-ANL-02). Protects against "column s.status does not exist"
/// when DB schema does not include status on study_sessions (e.g. per Docs/Entities.md).
/// </summary>
public class AnalyticsServiceHeatmapTests
{
    [Fact]
    public async Task GetHeatmapAsync_should_return_heatmap_with_activity_when_sessions_exist_in_range()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var year = 2025;

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            var sessionDate = new DateTime(year, 6, 15, 10, 0, 0, DateTimeKind.Utc);
            arrangeContext.StudySessions.Add(new StudySession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProjectId = projectId,
                StartTime = sessionDate.AddHours(-1),
                EndTime = sessionDate,
                CardsReviewed = 25,
                DurationSec = 600,
                NewLearned = 5,
                Status = "COMPLETED"
            });

            await arrangeContext.SaveChangesAsync();
        }

        await using (var actContext = new TestVocabularyServiceContext(options))
        {
            var logger = new Mock<ILogger<AnalyticsService>>();
            var userSettings = new UserSettingsService(actContext, new Mock<ILogger<UserSettingsService>>().Object);
            var sut = new AnalyticsService(actContext, logger.Object, userSettings);

            var result = await sut.GetHeatmapAsync(userId, projectId, year, CancellationToken.None);

            result.Should().NotBeNull();
            result.Year.Should().Be(year);
            result.ProjectId.Should().Be(projectId);
            result.TotalReviews.Should().Be(25);
            result.Activity.Should().NotBeEmpty();
            result.LongestStreak.Should().Be(1);
        }
    }

    private sealed class TestVocabularyServiceContext : VocabularyServiceContext
    {
        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options) : base(options) { }

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Card>().Ignore(c => c.SearchVector);
        }
    }
}
