using System.Linq;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AggregatorService.Tests;

public class AggregatorWebApplicationFactory : WebApplicationFactory<Program>
{
    public MockHolder VocabularyClientMockHolder { get; } = new MockHolder();
    public AgentClientMockHolder AgentClientMockHolder { get; } = new AgentClientMockHolder();
    public BillingClientMockHolder BillingClientMockHolder { get; } = new BillingClientMockHolder();

    /// <summary>
    /// When true, enables AI/advanced feature flags for tests that exercise those controllers.
    /// Production defaults remain false (fail-closed).
    /// </summary>
    public bool EnableAiAgentsForTests { get; set; }

    public bool EnableAdvancedModulesForTests { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            if (!EnableAiAgentsForTests && !EnableAdvancedModulesForTests)
                return;

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:EnableAIAgents"] = EnableAiAgentsForTests ? "true" : "false",
                ["Features:EnableAdvancedModules"] = EnableAdvancedModulesForTests ? "true" : "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var vocabularyDescriptors = services.Where(d => d.ServiceType == typeof(IVocabularyServiceClient)).ToList();
            foreach (var d in vocabularyDescriptors)
                services.Remove(d);
            services.AddSingleton(VocabularyClientMockHolder);
            services.AddTransient<IVocabularyServiceClient>(sp =>
                sp.GetRequiredService<MockHolder>().Current.Object);

            var agentDescriptors = services.Where(d => d.ServiceType == typeof(IAgentServiceClient)).ToList();
            foreach (var d in agentDescriptors)
                services.Remove(d);
            services.AddSingleton(AgentClientMockHolder);
            services.AddTransient<IAgentServiceClient>(sp =>
                sp.GetRequiredService<AgentClientMockHolder>().Current.Object);

            var billingDescriptors = services.Where(d => d.ServiceType == typeof(IBillingServiceClient)).ToList();
            foreach (var d in billingDescriptors)
                services.Remove(d);
            services.AddSingleton(BillingClientMockHolder);
            services.AddTransient<IBillingServiceClient>(sp =>
                sp.GetRequiredService<BillingClientMockHolder>().Current.Object);

            services.AddAuthentication(TestAuthHandler.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.TestScheme, _ => { });
            services.Configure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.TestScheme;
                o.DefaultChallengeScheme = TestAuthHandler.TestScheme;
                o.DefaultSignInScheme = TestAuthHandler.TestScheme;
            });
        });
    }
}

public class MockHolder
{
    private Mock<IVocabularyServiceClient> _current = new Mock<IVocabularyServiceClient>();

    public Mock<IVocabularyServiceClient> Current
    {
        get => _current;
        set => _current = value ?? new Mock<IVocabularyServiceClient>();
    }
}

public class AgentClientMockHolder
{
    private Mock<IAgentServiceClient> _current = new Mock<IAgentServiceClient>();

    public Mock<IAgentServiceClient> Current
    {
        get => _current;
        set => _current = value ?? new Mock<IAgentServiceClient>();
    }
}

public class BillingClientMockHolder
{
    private Mock<IBillingServiceClient> _current = new Mock<IBillingServiceClient>();

    public Mock<IBillingServiceClient> Current
    {
        get => _current;
        set => _current = value ?? new Mock<IBillingServiceClient>();
    }
}
