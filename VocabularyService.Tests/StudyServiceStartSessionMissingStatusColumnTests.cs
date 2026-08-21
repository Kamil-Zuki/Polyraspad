using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Services;
using Xunit;

namespace VocabularyService.Tests;

public class StudyServiceStartSessionMissingStatusColumnTests
{
    [Fact]
    public async Task should_start_session_when_study_sessions_status_column_is_missing()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var attachCommand = connection.CreateCommand())
        {
            attachCommand.CommandText = "ATTACH DATABASE ':memory:' AS internal;";
            await attachCommand.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<VocabularyServiceContext>()
            .UseSqlite(connection)
            .Options;

        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        await using (var setupContext = new SqliteVocabularyServiceContext(options))
        {
            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE internal.projects (
                    id TEXT PRIMARY KEY,
                    user_id TEXT NOT NULL,
                    title TEXT NOT NULL,
                    source_lang TEXT NOT NULL,
                    target_lang TEXT NOT NULL,
                    fsrs_settings TEXT NOT NULL,
                    tts_settings TEXT NULL,
                    stats TEXT NOT NULL,
                    is_archived INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                """);

            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE internal.study_sessions (
                    id TEXT PRIMARY KEY,
                    user_id TEXT NOT NULL,
                    project_id TEXT NOT NULL,
                    deck_id TEXT NULL,
                    start_time TEXT NOT NULL,
                    end_time TEXT NOT NULL,
                    cards_reviewed INTEGER NOT NULL,
                    duration_sec INTEGER NOT NULL,
                    new_learned INTEGER NOT NULL
                );
                """);

            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE internal.decks (
                    id TEXT PRIMARY KEY,
                    project_id TEXT NOT NULL,
                    parent_deck_id TEXT NULL,
                    owner_id TEXT NOT NULL,
                    is_public INTEGER NOT NULL
                );
                """);

            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE internal.cards (
                    id TEXT PRIMARY KEY,
                    deck_id TEXT NOT NULL,
                    creator_id TEXT NOT NULL
                );
                """);

            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE internal.user_card_progress (
                    id TEXT PRIMARY KEY,
                    user_id TEXT NOT NULL,
                    card_id TEXT NOT NULL,
                    project_id TEXT NOT NULL,
                    state INTEGER NOT NULL,
                    step INTEGER NOT NULL,
                    stability REAL NOT NULL,
                    difficulty REAL NOT NULL,
                    due TEXT NOT NULL,
                    elapsed_days INTEGER NOT NULL,
                    scheduled_days INTEGER NOT NULL,
                    reps INTEGER NOT NULL,
                    lapses INTEGER NOT NULL,
                    is_suspended INTEGER NOT NULL,
                    last_review TEXT NOT NULL
                );
                """);

            setupContext.Projects.Add(new Project
            {
                Id = projectId,
                UserId = userId,
                Title = "Project",
                SourceLang = "en",
                TargetLang = "jp",
                FsrsSettings = new FsrsSettings(),
                Stats = new ProjectStats(),
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await setupContext.Database.ExecuteSqlAsync(
                $"INSERT INTO internal.decks (id, project_id, parent_deck_id, owner_id, is_public) VALUES ({deckId}, {projectId}, NULL, {userId}, 0);");

            await setupContext.Database.ExecuteSqlAsync(
                $"INSERT INTO internal.cards (id, deck_id, creator_id) VALUES ({cardId}, {deckId}, {userId});");

            await setupContext.SaveChangesAsync();
        }

        await using var context = new SqliteVocabularyServiceContext(options);
        var userSettingsServiceMock = new Mock<IUserSettingsService>(MockBehavior.Strict);
        userSettingsServiceMock
            .Setup(service => service.GetUserSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSetting
            {
                UserId = userId,
                DailyGoalNew = 1,
                DailyGoalReview = 1,
                InterfaceLanguage = "en",
                UpdatedAt = DateTime.UtcNow
            });
        var sut = StudyServiceTestFactory.Create(
            context,
            Mock.Of<ICardService>(),
            Mock.Of<IFsrsScheduler>(),
            userSettingsServiceMock.Object,
            Mock.Of<IMediaService>());

        var result = await sut.StartStudySessionAsync(userId, projectId, null, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.Status.Should().Be("ACTIVE");

        await using var verificationCommand = connection.CreateCommand();
        verificationCommand.CommandText =
            """
            SELECT id, status
            FROM internal.study_sessions
            ORDER BY rowid DESC
            LIMIT 1;
            """;

        await using var reader = await verificationCommand.ExecuteReaderAsync();
        var hasRow = await reader.ReadAsync();
        hasRow.Should().BeTrue("a study session row should be persisted");
        reader.GetString(0).Should().NotBeNullOrWhiteSpace();
        reader.GetString(1).Should().Be("ACTIVE");
    }

    private sealed class SqliteVocabularyServiceContext : VocabularyServiceContext
    {
        public SqliteVocabularyServiceContext(DbContextOptions<VocabularyServiceContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<VocabularyService.Data.Entities.Card>().Ignore(c => c.SearchVector);
        }
    }
}
