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

public class TextServiceTermStatusTests
{
    [Fact]
    public async Task AnalyzeTextAsync_UsesRealWordFormsForStatuses()
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

            var sleep = AddTerm(arrangeContext, projectId, "sleep");
            var slept = AddTerm(arrangeContext, projectId, "slept");

            arrangeContext.UserTermStatuses.AddRange(
                AddStatus(userId, projectId, sleep.Id, "KNOWN"),
                AddStatus(userId, projectId, slept.Id, "SAVED"));

            await arrangeContext.SaveChangesAsync();
        }

        await using var actContext = CreateContext(dbName);
        var sut = new TextService(actContext, NullLogger<TextService>.Instance);

        var result = await sut.AnalyzeTextAsync(
            userId,
            new AnalyzeTextRequestDto
            {
                ProjectId = projectId,
                Text = "I sleep. I slept."
            });

        var sleepToken = result.Tokens.Single(token => token.Text == "sleep");
        var sleptToken = result.Tokens.Single(token => token.Text == "slept");

        sleepToken.Status.Should().Be(TokenStatus.Known);
        sleptToken.Status.Should().Be(TokenStatus.Learning);
        sleepToken.TermText.Should().Be("sleep");
        sleptToken.TermText.Should().Be("slept");
        result.Stats.UniqueWords.Should().Be(3);
    }

    [Fact]
    public async Task AnalyzeTextAsync_LegacyLingqStatusInDb_MapsToLearning()
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

            var word = AddTerm(arrangeContext, projectId, "legacy");
            arrangeContext.UserTermStatuses.Add(AddStatus(userId, projectId, word.Id, "LINGQ"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var actContext = CreateContext(dbName);
        var sut = new TextService(actContext, NullLogger<TextService>.Instance);

        var result = await sut.AnalyzeTextAsync(
            userId,
            new AnalyzeTextRequestDto
            {
                ProjectId = projectId,
                Text = "legacy word"
            });

        var token = result.Tokens.Single(t => t.Text == "legacy");
        token.Status.Should().Be(TokenStatus.Learning);
    }

    private static ProjectTerm AddTerm(VocabularyServiceContext context, Guid projectId, string text)
    {
        var term = new ProjectTerm
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Text = text,
            NormalizedText = TermNormalizer.Normalize(text),
            Type = "WORD",
            Language = "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ProjectTerms.Add(term);
        return term;
    }

    private static UserTermStatus AddStatus(Guid userId, Guid projectId, Guid termId, string status)
    {
        return new UserTermStatus
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            ProjectTermId = termId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
    }

    private static VocabularyServiceContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
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

