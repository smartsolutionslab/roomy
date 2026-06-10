using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints;

// The administrator-only account-management surface (contract: identity-api.md). Every route requires
// the administrator role, so an authenticated employee is Forbidden (403, FR-007). The service is
// internal — the BFF forwards the Keycloak token whose realm roles the host flattens to role claims.
public static class AdminUserEndpoints
{
    private const string AdministratorRole = "administrator";

    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/admin/users", ListAccountsAsync)
            .RequireAdministrator()
            .WithName("ListUsers")
            .Produces<Response.Page.AdminUser>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        endpoints.MapGet("/admin/users/{userId:guid}", GetAccountAsync)
            .RequireAdministrator()
            .WithName("GetUser")
            .Produces<Response.AdminUser>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        endpoints.MapPost("/admin/users/{userId:guid}:grant-administrator", GrantAdministratorAsync)
            .RequireAdministrator()
            .WithName("GrantAdministrator")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        return endpoints;
    }

    private static RouteHandlerBuilder RequireAdministrator(this RouteHandlerBuilder builder) =>
        builder.RequireAuthorization(policy => policy.RequireRole(AdministratorRole));

    // GET /admin/users — the account overview (identity-api.md). Administrator-only; keyset-paginated
    // by email (ADR-0044). A bad limit or a malformed cursor is a 400 (request validation, not a
    // domain rule).
    private static async Task<IResult> ListAccountsAsync(
        string? cursor,
        int? limit,
        IUserRepository users,
        CancellationToken cancellationToken)
    {
        var request = PageRequest.From(cursor, limit);
        if (request.IsFailure)
        {
            return request.Error.ToBadRequest();
        }

        var page = await users.GetPageAsync(request.Value, cancellationToken);

        return page.Match(accounts => Results.Ok(accounts.ToResponse()), error => error.ToBadRequest());
    }

    // GET /admin/users/{userId} — a single account, or 404 if no account has that identifier.
    private static async Task<IResult> GetAccountAsync(
        Guid userId, IUserRepository users, CancellationToken cancellationToken)
    {
        var lookup = await users.GetByIdentifierAsync(UserIdentifier.From(userId), cancellationToken);
        return lookup.Match(user => Results.Ok(user.ToAdminUser()), error => error.ToHttpResult());
    }

    // POST /admin/users/{userId}:grant-administrator — elevates the account (IA-4). Idempotent: a
    // repeated grant still returns 204. 404 for an unknown account.
    private static async Task<IResult> GrantAdministratorAsync(
        Guid userId,
        ICommandHandler<GrantAdministrator> grantAdministrator,
        CancellationToken cancellationToken)
    {
        var result = await grantAdministrator.HandleAsync(
            new GrantAdministrator(UserIdentifier.From(userId)), cancellationToken);

        return result.Match(Results.NoContent, error => error.ToHttpResult());
    }
}
