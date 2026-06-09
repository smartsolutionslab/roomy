using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Application.UseCases;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

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
            .Produces<IEnumerable<AdminUserResponse>>();
        endpoints.MapGet("/admin/users/{userId:guid}", GetAccountAsync)
            .RequireAdministrator()
            .WithName("GetUser")
            .Produces<AdminUserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        endpoints.MapPost("/admin/users/{userId:guid}:grant-administrator", GrantAdministratorAsync)
            .RequireAdministrator()
            .WithName("GrantAdministrator")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static RouteHandlerBuilder RequireAdministrator(this RouteHandlerBuilder builder) =>
        builder.RequireAuthorization(policy => policy.RequireRole(AdministratorRole));

    // GET /admin/users — the account overview (identity-api.md). Administrator-only.
    private static async Task<IResult> ListAccountsAsync(
        IUserRepository users, CancellationToken cancellationToken)
    {
        var accounts = await users.GetAllAsync(cancellationToken);
        return Results.Ok(accounts.Select(Project));
    }

    // GET /admin/users/{userId} — a single account, or 404 if no account has that identifier.
    private static async Task<IResult> GetAccountAsync(
        Guid userId, IUserRepository users, CancellationToken cancellationToken)
    {
        var lookup = await users.GetByIdentifierAsync(UserIdentifier.From(userId), cancellationToken);
        return lookup.Match(user => Results.Ok(Project(user)), _ => Results.NotFound());
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

        return result.Match(
            Results.NoContent,
            error => error.Type == ErrorType.NotFound ? Results.NotFound() : Results.Problem(error.Message));
    }

    private static AdminUserResponse Project(User user) =>
        new(
            user.Identifier.Value,
            user.Email.Value,
            user.DisplayName.Value,
            user.IsAdministrator ? "administrator" : "employee",
            user.Status == UserStatus.Active ? "active" : "provisioning");
}
