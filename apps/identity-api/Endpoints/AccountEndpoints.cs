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
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static async Task<IResult> GetCurrentAccountAsync(ClaimsPrincipal principal, IUserRepository users, CancellationToken cancellationToken)
    {
        var subject = principal.Subject();
        if (subject.IsFailure) return Results.Unauthorized();

        var keycloakSubject = KeycloakSubjectIdentifier.From(subject.Value);
        var lookup = await users.GetByKeycloakSubjectAsync(keycloakSubject, cancellationToken);

        return lookup.Match(user => Results.Ok(user.ToAccount()), error => error.ToHttpResult());
    }
}
