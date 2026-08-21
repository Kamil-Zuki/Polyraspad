using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Dtos.Text;
using VocabularyService.Helpers;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class TextServicePhraseTests
{
    [Fact]
    public async Task AnalyzeTextAsync_ReturnsSavedPhraseSpan_TakeOffNotSeparateWords()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await using (var arrangeContext = CreateContext(dbName))
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            var phrase = new ProjectTerm
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Text = "take off",
                NormalizedText = "take off",
                Type = "PHRASE",
                Language = "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            arrangeContext.ProjectTerms.Add(phrase);
            arrangeContext.UserTermStatuses.Add(new UserTermStatus
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProjectId = projectId,
                ProjectTermId = phrase.Id,
                Status = "SAVED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            });
            await arrangeContext.SaveChangesAsync();
        }

        await using var actContext = CreateContext(dbName);
        var sut = new TextService(actContext, NullLogger<TextService>.Instance);

        var result = await sut.AnalyzeTextAsync(
            userId,
            new AnalyzeTextRequestDto
            {
                ProjectId = projectId,
                Text = "They take off quickly."
            });

        result.Phrases.Should().ContainSingle();
        var span = result.Phrases[0];
        span.Text.Should().Contain("take");
        span.Text.Should().Contain("off");
        span.Status.Should().Be(TokenStatus.Learning);
        span.StartIndex.Should().BeLessThan(span.EndIndex);
    }

    private static VocabularyServiceContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestVocabularyServiceContext(options);
    }

    private sealed class TestVocabularyServiceContext : VocabularyServiceContext
    {
        public TestVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Card>().Ignore(card => card.SearchVector);
        }
    }
}
