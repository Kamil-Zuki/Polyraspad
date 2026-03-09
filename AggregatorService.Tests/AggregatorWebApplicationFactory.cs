using System.Linq;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AggregatorService.Tests;

/// <summary>
/// WebApplicationFactory that allows replacing IVocabularyServiceClient with a mock per test.
/// </summary>
public class AggregatorWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Holder for the current mock; replace before each request to control client behavior.
    /// </summary>
    public MockHolder VocabularyClientMockHolder { get; } = new MockHolder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(IVocabularyServiceClient)).ToList();
            foreach (var d in descriptors)
                services.Remove(d);
            services.AddSingleton(VocabularyClientMockHolder);
            services.AddTransient<IVocabularyServiceClient>(sp =>
                sp.GetRequiredService<MockHolder>().Current.Object);

            // Use test auth that reads X-User-Id so authenticated tests hit the controller (which returns 501)
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

/// <summary>
/// Holds the current Mock of IVocabularyServiceClient so tests can replace it per test.
/// </summary>
public class MockHolder
{
    private Mock<IVocabularyServiceClient> _current = new Mock<IVocabularyServiceClient>();

    public Mock<IVocabularyServiceClient> Current
    {
        get => _current;
        set => _current = value ?? new Mock<IVocabularyServiceClient>();
    }
}
