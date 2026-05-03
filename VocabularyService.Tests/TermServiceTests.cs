using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class TermServiceTests
{
    [Fact]
    public async Task CreateOrUpdateStatusAsync_CreatesWordStatusWithoutCard()
    {
        await using var context = CreateContext();
        var sut = new TermService(context);
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var status = await sut.CreateOrUpdateStatusAsync(
            userId,
            projectId,
            "  Slept  ",
            TermService.WordType,
            "EN",
            TermService.LingqStatus,
            meaning: "спал",
            firstSentence: "I slept well.");

        var term = await context.ProjectTerms.SingleAsync();
        var cardsCount = await context.Cards.CountAsync();

        term.Text.Should().Be("Slept");
        term.NormalizedText.Should().Be("slept");
        term.Type.Should().Be(TermService.WordType);
        term.Language.Should().Be("en");
        status.ProjectTermId.Should().Be(term.Id);
        status.Status.Should().Be(TermService.LingqStatus);
        status.Meaning.Should().Be("спал");
        status.FirstSentence.Should().Be("I slept well.");
        cardsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOrCreateTermAsync_KeepsWordAndPhraseTermsSeparate()
    {
        await using var context = CreateContext();
        var sut = new TermService(context);
        var projectId = Guid.NewGuid();

        var word = await sut.GetOrCreateTermAsync(projectId, "take", TermService.WordType, "en");
        var phrase = await sut.GetOrCreateTermAsync(projectId, "take off", TermService.PhraseType, "en");
        var samePhrase = await sut.GetOrCreateTermAsync(projectId, " take   off ", TermService.PhraseType, "en");
        await context.SaveChangesAsync();

        word.Id.Should().NotBe(phrase.Id);
        samePhrase.Id.Should().Be(phrase.Id);
        (await context.ProjectTerms.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetWordStatusesAsync_UsesExactNormalizedForms()
    {
        await using var context = CreateContext();
        var sut = new TermService(context);
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await sut.CreateOrUpdateStatusAsync(userId, projectId, "go", TermService.WordType, "en", TermService.KnownStatus);
        await sut.CreateOrUpdateStatusAsync(userId, projectId, "went", TermService.WordType, "en", TermService.LingqStatus);

        var statuses = await sut.GetWordStatusesAsync(userId, projectId, ["Go", "went", "gone"]);

        statuses["go"].Should().Be(VocabularyService.Dtos.Text.TokenStatus.Known);
        statuses["went"].Should().Be(VocabularyService.Dtos.Text.TokenStatus.Learning);
        statuses.Should().NotContainKey("gone");
    }

    private static VocabularyServiceContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

