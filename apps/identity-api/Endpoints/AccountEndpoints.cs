using System.Security.Claims;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints;

// The account/role read surface (contract: identity-api.md). The service is internal — reached only
// through the YARP BFF, which forwards the Keycloak access token — so the caller is identified by the
// token's subject (`sub`), mapped to the owning account.
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

    // GET /account/me — the current user's account/role projection (IA-2/IA-5). 401 is enforced by the
    // authorization policy; a valid session whose subject has no account record is a 404.
    private static async Task<IResult> GetCurrentAccountAsync(
        ClaimsPrincipal principal,
        IUserRepository users,
        CancellationToken cancellationToken)
    {
        var subjectClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (subjectClaim is null || !Guid.TryParse(subjectClaim, out var subjectValue))
        {
            return Results.Unauthorized();
        }

        var lookup = await users.GetByKeycloakSubjectAsync(
            KeycloakSubjectIdentifier.From(subjectValue), cancellationToken);

        return lookup.Match(
            user => Results.Ok(new Response.Account(
                user.Identifier.Value,
                user.Email.Value,
                user.DisplayName.Value,
                user.IsAdministrator ? "administrator" : "employee")),
            error => error.ToHttpResult());
    }
}
