using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartSolutionsLab.Roomy.TestSupport;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string SubjectHeader = "X-Test-Subject";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubjectHeader, out var subject) || string.IsNullOrEmpty(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // The real token carries both the Keycloak sub and the Roomy UserIdentifier (roomy_user_id,
        // ADR-0058). Set both to the test subject so endpoints resolve the caller whether they read
        // Subject() (identity, by Keycloak subject) or UserId() (attendance, by Roomy UserId).
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject.ToString()),
            new("roomy_user_id", subject.ToString()),
        };
        if (Request.Headers.TryGetValue(RolesHeader, out var roles) && !string.IsNullOrEmpty(roles))
        {
            var separators = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
            claims.AddRange(roles.ToString().Split(',', separators).Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
