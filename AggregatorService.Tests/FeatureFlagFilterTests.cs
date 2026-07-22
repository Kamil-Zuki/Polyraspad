using System.Net;
using System.Net.Http.Json;
using AggregatorService.Dtos;
using AggregatorService.Filters;
using AggregatorService.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AggregatorService.Tests;

public class FeatureFlagFilterTests
{
    [Fact]
    public void OnActionExecuting_WhenEnableAIAgentsFalse_Returns404()
    {
        var filter = new FeatureFlagFilterAttribute("EnableAIAgents");
        var context = CreateActionExecutingContext(new FeaturesOptions
        {
            EnableAIAgents = false,
            EnableAdvancedModules = false,
        });

        filter.OnActionExecuting(context);

        var result = context.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void OnActionExecuting_WhenEnableAIAgentsTrue_DoesNotSetResult()
    {
        var filter = new FeatureFlagFilterAttribute("EnableAIAgents");
        var context = CreateActionExecutingContext(new FeaturesOptions
        {
            EnableAIAgents = true,
            EnableAdvancedModules = false,
        });

        filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAudio_WhenAiAgentsDisabled_Returns404()
    {
        await using var factory = new AggregatorWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "11111111-1111-1111-1111-111111111111");

        var response = await client.PostAsJsonAsync(
            "/api/Media/generate-audio",
            new GenerateAudioRequestDto { Text = "hello", Language = "en" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.Should().NotBeNull();
        body!["error"].Should().Be("Feature is disabled");
    }

    private static ActionExecutingContext CreateActionExecutingContext(FeaturesOptions features)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptionsSnapshot<FeaturesOptions>>(new TestOptionsSnapshot(features));
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private sealed class TestOptionsSnapshot(FeaturesOptions value) : IOptionsSnapshot<FeaturesOptions>
    {
        public FeaturesOptions Value { get; } = value;
        public FeaturesOptions Get(string? name) => Value;
    }
}
