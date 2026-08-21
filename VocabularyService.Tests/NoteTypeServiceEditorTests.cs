using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class NoteTypeServiceEditorTests
{
    [Fact]
    public async Task GetSentenceMiningForEditorAsync_CreatesNoteTypeFieldsAndDefaultTemplate()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            ArrangeProjectAndDeck(arrange, userId, projectId, deckId);
            await arrange.SaveChangesAsync();
        }

        await using (var act = CreateContext(dbName))
        {
            var sut = new NoteTypeService(act);
            var nt = await sut.GetSentenceMiningForEditorAsync(userId, projectId);

            nt.Name.Should().Be(SentenceMiningNoteType.TypeName);
            nt.NoteFields.Should().HaveCount(14);
            nt.NoteFields.Should().Contain(f => f.FieldKey == SentenceMiningNoteType.Expression && f.Required);
            nt.CardTemplates.Should().ContainSingle(t => t.TemplateKey == SentenceMiningNoteType.DefaultTemplateKey);
            var def = nt.CardTemplates.Single(t => t.TemplateKey == SentenceMiningNoteType.DefaultTemplateKey);
            def.FrontTemplate.Should().Contain("{{Expression}}");
            def.BackTemplate.Should().Contain("{{Translation}}");
        }
    }

    [Fact]
    public async Task GetSentenceMiningForEditorAsync_RejectsOtherUser()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await using (var arrange = CreateContext(dbName))
        {
            ArrangeProjectAndDeck(arrange, ownerId, projectId, deckId);
            await arrange.SaveChangesAsync();
        }

        await using (var act = CreateContext(dbName))
        {
            var sut = new NoteTypeService(act);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.GetSentenceMiningForEditorAsync(otherId, projectId));
        }
    }

    private static VocabularyServiceContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestVocabularyServiceContext(options);
    }

    private static void ArrangeProjectAndDeck(
        VocabularyServiceContext context,
        Guid userId,
        Guid projectId,
        Guid deckId)
    {
        context.Projects.Add(new Project
        {
            Id = projectId,
            UserId = userId,
            Title = "Test Project",
            SourceLang = "en",
            TargetLang = "ru",
            FsrsSettings = new FsrsSettings(),
            Stats = new ProjectStats(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Decks.Add(new Deck
        {
            Id = deckId,
            ProjectId = projectId,
            OwnerId = userId,
            Title = "Test Deck",
            Description = null,
            CoverImageUrl = null,
            IsPublic = false,
            ContributionPolicy = "OPEN",
            LicenseType = "PRIVATE",
            ForkedFromId = null,
            CardCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
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
