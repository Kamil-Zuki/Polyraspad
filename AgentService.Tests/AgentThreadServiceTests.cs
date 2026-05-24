using AgentService.Data;
using AgentService.Data.Entities;
using AgentService.Dtos.Agent;
using AgentService.Helpers;
using AgentService.Orchestration;
using AgentService.Options;
using AgentService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pvs.Content.Grpc;
using Xunit;

namespace AgentService.Tests;

public class AgentThreadServiceTests
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    [Fact]
    public async Task ListThreads_Returns_NonArchived_Ordered_By_UpdatedAt_Desc()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;

        await using (var arrange = new AgentServiceContext(options))
        {
            arrange.AgentThreads.AddRange(
                CreateThread(UserA, ProjectId, now.AddMinutes(-10), archived: false, title: "Older"),
                CreateThread(UserA, ProjectId, now, archived: false, title: "Latest"),
                CreateThread(UserA, ProjectId, now.AddMinutes(-5), archived: true, title: "Archived"));
            await arrange.SaveChangesAsync();
        }

        await using var act = new AgentServiceContext(options);
        var sut = CreateSut(act);

        var threads = await sut.ListThreadsAsync(UserA, ProjectId, ["user"]);

        threads.Should().HaveCount(2);
        threads[0].Title.Should().Be("Latest");
        threads[1].Title.Should().Be("Older");
    }

    [Fact]
    public async Task GetThread_CrossUser_Returns_Null()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;
        var threadId = Guid.NewGuid();

        await using (var arrange = new AgentServiceContext(options))
        {
            arrange.AgentThreads.Add(CreateThread(UserA, ProjectId, now, archived: false, title: "Mine", id: threadId));
            await arrange.SaveChangesAsync();
        }

        await using var act = new AgentServiceContext(options);
        var sut = CreateSut(act);

        var thread = await sut.GetThreadAsync(UserB, threadId);
        thread.Should().BeNull();
    }

    [Fact]
    public async Task ListMessages_Returns_CreatedAt_Ascending_Order()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;
        var threadId = Guid.NewGuid();

        await using (var arrange = new AgentServiceContext(options))
        {
            arrange.AgentThreads.Add(CreateThread(UserA, ProjectId, now, archived: false, title: "Chat", id: threadId));
            arrange.AgentMessages.AddRange(
                new AgentMessage { Id = Guid.NewGuid(), ThreadId = threadId, Role = "user", Content = "first", CreatedAt = now.AddMinutes(-2) },
                new AgentMessage { Id = Guid.NewGuid(), ThreadId = threadId, Role = "assistant", Content = "second", CreatedAt = now.AddMinutes(-1) });
            await arrange.SaveChangesAsync();
        }

        await using var act = new AgentServiceContext(options);
        var sut = CreateSut(act);

        var messages = await sut.ListMessagesAsync(UserA, threadId, 100, beforeMessageId: null);
        messages.Should().NotBeNull();
        messages!.Items.Should().HaveCount(2);
        messages.Items[0].Content.Should().Be("first");
        messages.Items[1].Content.Should().Be("second");
    }

    [Fact]
    public async Task CreateRun_Persists_OutOfScope_DomainDecision_And_Title()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;
        var threadId = Guid.NewGuid();

        await using (var arrange = new AgentServiceContext(options))
        {
            arrange.AgentThreads.Add(CreateThread(UserA, ProjectId, now, archived: false, title: null, id: threadId));
            await arrange.SaveChangesAsync();
        }

        await using var act = new AgentServiceContext(options);
        var sut = CreateSut(act);

        var result = await sut.CreateRunAsync(UserA, threadId, ProjectId, new CreateAgentRunDto
        {
            UserMessage = new AgentMessageInputDto { Role = "user", Content = "Write me Python homework please" },
            AssistantMessage = new AgentMessageInputDto
            {
                Role = "assistant",
                Content = "I can only help with language learning.",
                MetadataJson = """{"refusal":true,"intentCategory":"out_of_scope"}"""
            },
            DomainDecision = new AgentDomainDecisionInputDto
            {
                Allowed = false,
                Category = "out_of_scope",
                Reason = "Programming homework"
            },
            ToolCalls = Array.Empty<AgentToolCallInputDto>()
        });

        result.Should().NotBeNull();
        result!.Run.Status.Should().Be("completed");

        var thread = await act.AgentThreads.SingleAsync(t => t.Id == threadId);
        thread.Title.Should().Be(AgentThreadTitleHelper.DeriveTitle("Write me Python homework please"));

        var decision = await act.AgentDomainDecisions.SingleAsync();
        decision.Allowed.Should().BeFalse();
        decision.Category.Should().Be("out_of_scope");
        decision.UserTextPreview.Should().Contain("Python homework");
    }

    [Fact]
    public async Task ArchiveThread_Excludes_Thread_From_List()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;
        var threadId = Guid.NewGuid();

        await using (var arrange = new AgentServiceContext(options))
        {
            arrange.AgentThreads.Add(CreateThread(UserA, ProjectId, now, archived: false, title: "To archive", id: threadId));
            await arrange.SaveChangesAsync();
        }

        await using var act = new AgentServiceContext(options);
        var sut = CreateSut(act);

        (await sut.ArchiveThreadAsync(UserA, threadId)).Should().BeTrue();

        var threads = await sut.ListThreadsAsync(UserA, ProjectId, ["user"]);
        threads.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateRun_WrongProject_Returns_Null()
    {
        var options = CreateOptions();
        var now = DateTime.UtcNow;
        var threadId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();

        await using (var arrange = new AgentServiceContext(options))
        {
            arrange.AgentThreads.Add(CreateThread(UserA, ProjectId, now, archived: false, title: "Chat", id: threadId));
            await arrange.SaveChangesAsync();
        }

        await using var act = new AgentServiceContext(options);
        var sut = CreateSut(act);

        var result = await sut.CreateRunAsync(UserA, threadId, otherProjectId, ValidRunRequest());
        result.Should().BeNull();
    }

    private static CreateAgentRunDto ValidRunRequest() => new()
    {
        UserMessage = new AgentMessageInputDto { Role = "user", Content = "How do I say hello?" },
        AssistantMessage = new AgentMessageInputDto { Role = "assistant", Content = "Hola" },
        DomainDecision = new AgentDomainDecisionInputDto { Allowed = true, Category = "language_learning" },
        ToolCalls = Array.Empty<AgentToolCallInputDto>()
    };

    public static AgentThreadService CreateSut(AgentServiceContext context)
    {
        var validator = new Mock<IVocabularyProjectAccessValidator>();
        validator.Setup(v => v.EnsureProjectAccessAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectResponse
            {
                Id = ProjectId.ToString(),
                UserId = UserA.ToString(),
                Title = "English",
                SourceLang = "en",
                TargetLang = "ru"
            });

        return new AgentThreadService(context, validator.Object, new Mock<ILogger<AgentThreadService>>().Object);
    }

    private static DbContextOptions<AgentServiceContext> CreateOptions() =>
        new DbContextOptionsBuilder<AgentServiceContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static AgentThread CreateThread(
        Guid userId,
        Guid projectId,
        DateTime updatedAt,
        bool archived,
        string? title,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserId = userId,
        ProjectId = projectId,
        Title = title,
        CreatedAt = updatedAt,
        UpdatedAt = updatedAt,
        ArchivedAt = archived ? updatedAt : null
    };
}

public class AgentOrchestratorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    [Fact]
    public async Task ExecuteRun_OutOfScope_Persists_Refusal()
    {
        var options = new DbContextOptionsBuilder<AgentServiceContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var threadId = Guid.NewGuid();
        await using (var arrange = new AgentServiceContext(options))
        {
            arrange.AgentThreads.Add(new AgentThread
            {
                Id = threadId,
                UserId = UserId,
                ProjectId = ProjectId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await arrange.SaveChangesAsync();
        }

        await using var context = new AgentServiceContext(options);
        var threadService = AgentThreadServiceTests.CreateSut(context);

        var projectValidator = new Mock<IVocabularyProjectAccessValidator>();
        projectValidator.Setup(v => v.EnsureProjectAccessAsync(
                UserId,
                ProjectId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectResponse
            {
                Id = ProjectId.ToString(),
                UserId = UserId.ToString(),
                Title = "English",
                SourceLang = "en",
                TargetLang = "ru"
            });

        var orchestrator = new AgentOrchestrator(
            threadService,
            projectValidator.Object,
            Mock.Of<IVocabularyGrpcClient>(),
            Mock.Of<IAgentLlmProvider>(),
            Microsoft.Extensions.Options.Options.Create(new AiOptions { Enabled = false }),
            Mock.Of<ILogger<AgentOrchestrator>>());

        var result = await orchestrator.ExecuteRunAsync(
            UserId,
            threadId,
            ProjectId,
            new ExecuteAgentRunDto { UserText = "Write me Python homework please" },
            ["user"]);

        result.Should().NotBeNull();
        result!.AssistantMessage.Content.Should().Contain("language learning");
        result.AssistantMessage.MetadataJson.Should().Contain("out_of_scope");

        var decision = await context.AgentDomainDecisions.SingleAsync();
        decision.Allowed.Should().BeFalse();
        decision.Category.Should().Be("out_of_scope");
    }
}
