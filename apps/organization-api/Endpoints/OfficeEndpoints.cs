using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.UseCases;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

// The office/room management surface (contract: organization-api.md). Writes require the administrator
// role, so an authenticated employee is Forbidden (403, FR-009); reads require any authenticated
// account. The service is internal — the BFF forwards the Keycloak token whose realm roles the host
// flattens to role claims.
public static class OfficeEndpoints
{
    private const string AdministratorRole = "administrator";

    public static IEndpointRouteBuilder MapOfficeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/offices", CreateOfficeAsync).RequireAdministrator();
        endpoints.MapGet("/offices", ListOfficesAsync).RequireAuthorization();
        endpoints.MapGet("/offices/{officeId:guid}", GetOfficeAsync).RequireAuthorization();

        return endpoints;
    }

    private static RouteHandlerBuilder RequireAdministrator(this RouteHandlerBuilder builder) =>
        builder.RequireAuthorization(policy => policy.RequireRole(AdministratorRole));

    // POST /offices — creates an office under the seeded company. 400 for a blank name/location, 409 if
    // the name is already taken in the company.
    private static async Task<IResult> CreateOfficeAsync(
        CreateOfficeRequest request,
        ICommandHandler<CreateOffice, OfficeIdentifier> createOffice,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var name = OfficeName.TryParse(request.Name);
        var location = Location.TryParse(request.Location);
        if (name is null || location is null)
        {
            return Results.BadRequest("An office requires a non-empty name and location.");
        }

        var result = await createOffice.HandleAsync(new CreateOffice(name, location), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.Conflict
                ? Results.Conflict(result.Error.Message)
                : Results.Problem(result.Error.Message);
        }

        var created = await offices.GetByIdentifierAsync(result.Value, cancellationToken);
        return created.Match(
            office => Results.Created($"/offices/{office.Identifier.Value}", Project(office)),
            error => Results.Problem(error.Message));
    }

    // GET /offices — every office with its rooms and derived capacity.
    private static async Task<IResult> ListOfficesAsync(
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var all = await offices.GetAllAsync(cancellationToken);
        return Results.Ok(all.Select(Project));
    }

    // GET /offices/{officeId} — a single office, or 404 if none has that identifier.
    private static async Task<IResult> GetOfficeAsync(
        Guid officeId,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var office = await offices.GetByIdentifierAsync(OfficeIdentifier.From(officeId), cancellationToken);
        return office.Match(found => Results.Ok(Project(found)), _ => Results.NotFound());
    }

    private static OfficeResponse Project(Office office) =>
        new(
            office.Identifier.Value,
            office.Name.Value,
            office.Location.Value,
            office.Capacity,
            office.Rooms
                .Select(room => new RoomResponse(room.Identifier.Value, room.Name.Value, room.Capacity))
                .ToList());
}
