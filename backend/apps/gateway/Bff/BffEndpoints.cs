using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using SmartSolutionsLab.Roomy.Gateway.Authentication;

namespace SmartSolutionsLab.Roomy.Gateway.Bff;

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

    private static bool IsSafeRelativeReturnUrl([NotNullWhen(true)] string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative);
}
