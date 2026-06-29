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
            .ProducesError(StatusCodes.Status400BadRequest);
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

    private static async Task<IResult> ListAccountsAsync(string? cursor, int? limit, IUserRepository users, CancellationToken cancellationToken)
    {
        var request = PageRequest.From(cursor, limit);

        var page = await users.GetPageAsync(request, cancellationToken);

        return Results.Ok(page.ToResponse());
    }

    private static async Task<IResult> GetAccountAsync(Guid userId, IUserRepository users, CancellationToken cancellationToken)
    {
        var userIdentifier = UserIdentifier.From(userId);
        var lookup = await users.GetByIdentifierAsync(userIdentifier, cancellationToken);
        return lookup.ToOk(user => user.ToAdminUser());
    }

    private static async Task<IResult> GrantAdministratorAsync(
        Guid userId,
        ICommandHandler<GrantAdministrator> commandHandler,
        CancellationToken cancellationToken)
    {
        var command = new GrantAdministrator(UserIdentifier.From(userId));
        var result = await commandHandler.HandleAsync(command, cancellationToken);

        return result.ToNoContent();
    }
}
