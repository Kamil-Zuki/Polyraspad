using System.Linq;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AggregatorService.Tests;

public class AggregatorWebApplicationFactory : WebApplicationFactory<Program>
{
    public MockHolder VocabularyClientMockHolder { get; } = new MockHolder();
    public AgentClientMockHolder AgentClientMockHolder { get; } = new AgentClientMockHolder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
