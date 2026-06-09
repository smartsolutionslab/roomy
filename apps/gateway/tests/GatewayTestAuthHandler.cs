using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

// Stands in for an authenticated BFF session so RequireAuthorization is satisfied without a live
// Keycloak. A request carrying the X-Test-Authenticated header is treated as signed in; without it the
// request is anonymous. The real cookie and OIDC schemes stay registered, so logout still clears the
// BFF session cookie and targets the OIDC sign-out handler.
internal sealed class GatewayTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string AuthenticatedHeader = "X-Test-Authenticated";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(AuthenticatedHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "test-subject")], SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
