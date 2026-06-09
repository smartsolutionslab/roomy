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
        endpoints.MapPatch("/offices/{officeId:guid}/name", RenameOfficeAsync).RequireAdministrator();
        endpoints.MapPatch("/offices/{officeId:guid}/location", ChangeLocationAsync).RequireAdministrator();
        endpoints.MapPost("/offices/{officeId:guid}/rooms", AddRoomAsync).RequireAdministrator();
        endpoints.MapPatch("/offices/{officeId:guid}/rooms/{roomId:guid}/name", RenameRoomAsync)
            .RequireAdministrator();

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

    // PATCH /offices/{officeId}/name — renames the office. 400 blank, 404 unknown, 409 name taken.
    private static async Task<IResult> RenameOfficeAsync(
        Guid officeId,
        RenameOfficeRequest request,
        ICommandHandler<RenameOffice> renameOffice,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var name = OfficeName.TryParse(request.Name);
        if (name is null)
        {
            return Results.BadRequest("An office name must not be blank.");
        }

        var identifier = OfficeIdentifier.From(officeId);
        var result = await renameOffice.HandleAsync(new RenameOffice(identifier, name), cancellationToken);
        return await OfficeResultAsync(result, identifier, offices, cancellationToken);
    }

    // PATCH /offices/{officeId}/location — relocates the office. 400 blank, 404 unknown.
    private static async Task<IResult> ChangeLocationAsync(
        Guid officeId,
        RelocateOfficeRequest request,
        ICommandHandler<ChangeOfficeLocation> changeLocation,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var location = Location.TryParse(request.Location);
        if (location is null)
        {
            return Results.BadRequest("An office location must not be blank.");
        }

        var identifier = OfficeIdentifier.From(officeId);
        var result = await changeLocation.HandleAsync(
            new ChangeOfficeLocation(identifier, location), cancellationToken);
        return await OfficeResultAsync(result, identifier, offices, cancellationToken);
    }

    // POST /offices/{officeId}/rooms — adds a room. 400 blank name or capacity < 1, 404 unknown office,
    // 409 room name taken.
    private static async Task<IResult> AddRoomAsync(
        Guid officeId,
        AddRoomRequest request,
        ICommandHandler<AddRoomToOffice, RoomIdentifier> addRoom,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var name = RoomName.TryParse(request.Name);
        var capacity = Capacity.TryParse(request.Capacity);
        if (name is null || capacity is null)
        {
            return Results.BadRequest("A room requires a non-empty name and a capacity of at least 1.");
        }

        var identifier = OfficeIdentifier.From(officeId);
        var result = await addRoom.HandleAsync(
            new AddRoomToOffice(identifier, name, capacity.Value), cancellationToken);
        if (result.IsFailure)
        {
            return MapError(result.Error);
        }

        var office = await offices.GetByIdentifierAsync(identifier, cancellationToken);
        return office.Match(
            found => found.Rooms.FirstOrDefault(room => room.Identifier == result.Value) is { } room
                ? Results.Created(
                    $"/offices/{officeId}/rooms/{room.Identifier.Value}",
                    new RoomResponse(room.Identifier.Value, room.Name.Value, room.Capacity))
                : Results.NotFound(),
            _ => Results.NotFound());
    }

    // PATCH /offices/{officeId}/rooms/{roomId}/name — renames a room. 400 blank, 404 unknown office/room,
    // 409 name taken within the office.
    private static async Task<IResult> RenameRoomAsync(
        Guid officeId,
        Guid roomId,
        RenameRoomRequest request,
        ICommandHandler<RenameRoom> renameRoom,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var name = RoomName.TryParse(request.Name);
        if (name is null)
        {
            return Results.BadRequest("A room name must not be blank.");
        }

        var identifier = OfficeIdentifier.From(officeId);
        var result = await renameRoom.HandleAsync(
            new RenameRoom(identifier, RoomIdentifier.From(roomId), name), cancellationToken);
        return await OfficeResultAsync(result, identifier, offices, cancellationToken);
    }

    // Maps a mutation result to the refreshed office (200), or the error to its status code.
    private static async Task<IResult> OfficeResultAsync(
        Result result,
        OfficeIdentifier officeIdentifier,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        if (result.IsFailure)
        {
            return MapError(result.Error);
        }

        var office = await offices.GetByIdentifierAsync(officeIdentifier, cancellationToken);
        return office.Match(found => Results.Ok(Project(found)), _ => Results.NotFound());
    }

    private static IResult MapError(Error error) => error.Type switch
    {
        ErrorType.NotFound => Results.NotFound(),
        ErrorType.Conflict => Results.Conflict(error.Message),
        _ => Results.Problem(error.Message),
    };

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
