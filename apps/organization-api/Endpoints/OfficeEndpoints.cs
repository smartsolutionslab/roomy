using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

public static class OfficeEndpoints
{
    private const string AdministratorRole = "administrator";

    public static IEndpointRouteBuilder MapOfficeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/offices", CreateOfficeAsync)
            .RequireAdministrator()
            .WithName("CreateOffice")
            .Produces<Response.Office>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);
        endpoints.MapGet("/offices", ListOfficesAsync)
            .RequireAuthorization()
            .WithName("ListOffices")
            .Produces<IEnumerable<Response.Office>>();
        endpoints.MapGet("/offices/{officeId:guid}", GetOfficeAsync)
            .RequireAuthorization()
            .WithName("GetOffice")
            .Produces<Response.Office>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        endpoints.MapPatch("/offices/{officeId:guid}/name", RenameOfficeAsync)
            .RequireAdministrator()
            .WithName("RenameOffice")
            .Produces<Response.Office>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);
        endpoints.MapPatch("/offices/{officeId:guid}/location", ChangeLocationAsync)
            .RequireAdministrator()
            .WithName("ChangeOfficeLocation")
            .Produces<Response.Office>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        endpoints.MapPost("/offices/{officeId:guid}/rooms", AddRoomAsync)
            .RequireAdministrator()
            .WithName("AddRoom")
            .Produces<Response.Room>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);
        endpoints.MapPatch("/offices/{officeId:guid}/rooms/{roomId:guid}/name", RenameRoomAsync)
            .RequireAdministrator()
            .WithName("RenameRoom")
            .Produces<Response.Office>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static RouteHandlerBuilder RequireAdministrator(this RouteHandlerBuilder builder) =>
        builder.RequireAuthorization(policy => policy.RequireRole(AdministratorRole));

    private static async Task<IResult> CreateOfficeAsync(
        Request.CreateOffice request,
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
            return result.Error.ToHttpResult();
        }

        var created = await offices.GetByIdentifierAsync(result.Value, cancellationToken);
        return created.Match(
            office => Results.Created($"/offices/{office.Identifier.Value}", office.ToResponse()),
            error => error.ToHttpResult());
    }

    private static async Task<IResult> ListOfficesAsync(
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var all = await offices.GetAllAsync(cancellationToken);
        return Results.Ok(all.Select(office => office.ToResponse()));
    }

    private static async Task<IResult> GetOfficeAsync(
        Guid officeId,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var office = await offices.GetByIdentifierAsync(OfficeIdentifier.From(officeId), cancellationToken);
        return office.Match(found => Results.Ok(found.ToResponse()), error => error.ToHttpResult());
    }

    private static async Task<IResult> RenameOfficeAsync(
        Guid officeId,
        Request.RenameOffice request,
        ICommandHandler<RenameOffice> renameOffice,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var name = OfficeName.TryParse(request.Name);
        if (name is null) return Results.BadRequest("An office name must not be blank.");

        var identifier = OfficeIdentifier.From(officeId);
        var result = await renameOffice.HandleAsync(new RenameOffice(identifier, name), cancellationToken);
        return await OfficeResultAsync(result, identifier, offices, cancellationToken);
    }

    private static async Task<IResult> ChangeLocationAsync(
        Guid officeId,
        Request.RelocateOffice request,
        ICommandHandler<ChangeOfficeLocation> changeLocation,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var location = Location.TryParse(request.Location);
        if (location is null) return Results.BadRequest("An office location must not be blank.");

        var identifier = OfficeIdentifier.From(officeId);
        var result = await changeLocation.HandleAsync(
            new ChangeOfficeLocation(identifier, location),
            cancellationToken);
        return await OfficeResultAsync(result, identifier, offices, cancellationToken);
    }

    private static async Task<IResult> AddRoomAsync(
        Guid officeId,
        Request.AddRoom request,
        ICommandHandler<AddRoomToOffice, RoomIdentifier> addRoom,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var name = RoomName.TryParse(request.Name);
        var capacity = Capacity.TryParse(request.Capacity);
        if (name is null || capacity is null) return Results.BadRequest("A room requires a non-empty name and a capacity of at least 1.");

        var identifier = OfficeIdentifier.From(officeId);
        var result = await addRoom.HandleAsync(
            new AddRoomToOffice(identifier, name, capacity.Value),
            cancellationToken);
        if (result.IsFailure) return result.Error.ToHttpResult();

        var office = await offices.GetByIdentifierAsync(identifier, cancellationToken);
        return office.Match(
            found => found.Rooms.FirstOrDefault(room => room.Identifier == result.Value) is { } room
                ? Results.Created($"/offices/{officeId}/rooms/{room.Identifier.Value}", room.ToResponse())
                : Results.NotFound(),
            _ => Results.NotFound());
    }

    private static async Task<IResult> RenameRoomAsync(
        Guid officeId,
        Guid roomId,
        Request.RenameRoom request,
        ICommandHandler<RenameRoom> renameRoom,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var name = RoomName.TryParse(request.Name);
        if (name is null) return Results.BadRequest("A room name must not be blank.");

        var identifier = OfficeIdentifier.From(officeId);
        var result = await renameRoom.HandleAsync(
            new RenameRoom(identifier, RoomIdentifier.From(roomId), name),
            cancellationToken);
        return await OfficeResultAsync(result, identifier, offices, cancellationToken);
    }

    private static async Task<IResult> OfficeResultAsync(
        Result result,
        OfficeIdentifier officeIdentifier,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        if (result.IsFailure) return result.Error.ToHttpResult();

        var office = await offices.GetByIdentifierAsync(officeIdentifier, cancellationToken);
        return office.Match(found => Results.Ok(found.ToResponse()), error => error.ToHttpResult());
    }
}
