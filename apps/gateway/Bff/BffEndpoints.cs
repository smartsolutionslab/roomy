using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using SmartSolutionsLab.Roomy.Gateway.Authentication;

namespace SmartSolutionsLab.Roomy.Gateway.Bff;

// The browser-facing auth surface of the BFF (ADR-0013). Login starts the server-side OIDC
// challenge; logout clears the session and triggers Keycloak RP-initiated end-session; the
// whoami endpoint returns the current user's name and roles — never any token.
public static class BffEndpoints
{
    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/bff");

        group.MapGet("/login", Login);
        group.MapPost("/logout", Logout).RequireAuthorization();
        group.MapGet("/user", WhoAmI);

        return endpoints;
    }

    // Begins the OIDC auth-code flow. `returnUrl` is the SPA path to land on afterwards; it is
    // validated as a local path to avoid open-redirects.
    private static IResult Login(HttpContext httpContext, string? returnUrl)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true) return Results.Redirect(SafeReturnUrl(returnUrl));

        var properties = new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) };
        return Results.Challenge(properties, [BffAuthenticationExtensions.OidcScheme]);
    }

    private static IResult Logout(HttpContext httpContext, string? returnUrl)
    {
        var properties = new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) };
        return Results.SignOut(properties, [BffAuthenticationExtensions.CookieScheme, BffAuthenticationExtensions.OidcScheme]);
    }

    private static IResult WhoAmI(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();

        return Results.Ok(httpContext.User.ToCurrentUser());
    }

    private static string SafeReturnUrl(string? returnUrl) =>
        IsSafeRelativeReturnUrl(returnUrl) ? returnUrl : "/";

    // Only a non-empty, well-formed relative URL is a safe redirect target — an absolute URL would be an
    // open-redirect vector, so anything else falls back to the app root.
    private static bool IsSafeRelativeReturnUrl([NotNullWhen(true)] string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative);
}
