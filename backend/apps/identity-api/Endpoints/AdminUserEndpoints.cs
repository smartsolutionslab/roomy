using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Identity.Api.Endpoints;

public static class AdminUserEndpoints
{
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

    private static async Task<IResult> GetAccountAsync(
        Guid userId, IUserRepository users, CancellationToken cancellationToken)
    {
        var lookup = await users.GetByIdentifierAsync(UserIdentifier.From(userId), cancellationToken);
        return lookup.Match(user => Results.Ok(user.ToAdminUser()), error => error.ToHttpResult());
    }

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
