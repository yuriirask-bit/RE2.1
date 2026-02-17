using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace RE2.ComplianceWeb.Authentication;

/// <summary>
/// Development-only authentication handler that auto-authenticates requests
/// with a mock ComplianceManager user. Only active when UseInMemoryRepositories is true.
/// DO NOT use in production!
/// </summary>
public class DevelopmentAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Development";

    public DevelopmentAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Create a mock user with ComplianceManager role for testing
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-001"),
            new Claim(ClaimTypes.Name, "Development User"),
            new Claim(ClaimTypes.Email, "dev@localhost"),
            new Claim(ClaimTypes.Role, "ComplianceManager"),
            new Claim(ClaimTypes.Role, "QAUser"),
            new Claim(ClaimTypes.Role, "SalesAdmin"),
            new Claim(ClaimTypes.Role, "TrainingCoordinator"),
            new Claim("name", "Development User"), // Azure AD style name claim
            new Claim("roles", "ComplianceManager"), // Azure AD style role claim
            new Claim("roles", "QAUser"),
            new Claim("roles", "SalesAdmin"),
            new Claim("roles", "TrainingCoordinator"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
