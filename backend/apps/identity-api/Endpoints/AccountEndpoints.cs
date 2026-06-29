using System.Security.Claims;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/account/me", GetCurrentAccountAsync)
            .RequireAuthorization()
            .WithName("GetCurrentAccount")
            .Produces<Response.Account>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static async Task<IResult> GetCurrentAccountAsync(ClaimsPrincipal principal, IUserRepository users, CancellationToken cancellationToken)
    {
        if (!principal.TryGetSubject(out var subject)) return Results.Unauthorized();

        var keycloakSubjectOfPrincipal = KeycloakSubjectIdentifier.From(subject);
        var userByPrincipal = await users.GetByKeycloakSubjectAsync(keycloakSubjectOfPrincipal, cancellationToken);

        return userByPrincipal.ToOk(user => user.ToAccount());
    }
}
