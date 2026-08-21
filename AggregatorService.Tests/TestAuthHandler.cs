using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AggregatorService.Tests;

/// <summary>
/// Authentication handler for tests: when X-User-Id header is present, authenticates as that user (Sub claim).
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
            string.IsNullOrWhiteSpace(userIdHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-User-Id"));
        }

        var userId = userIdHeader.ToString();
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, userId) };
        var identity = new ClaimsIdentity(claims, TestScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
