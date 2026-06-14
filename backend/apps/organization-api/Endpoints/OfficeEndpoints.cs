using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

public static class OfficeEndpoints
{
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

    private static async Task<IResult> CreateOfficeAsync(
        Request.CreateOffice request,
        ICommandHandler<CreateOffice, OfficeIdentifier> commandHandler,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var command = new CreateOffice(
            OfficeName.From(request.Name),
            Location.From(request.Location));
        var result = await commandHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return result.Error.ToHttpResult();

        var created = await offices.GetByIdentifierAsync(result.Value, cancellationToken);
        return created.Match(
            office => Results.Created($"/offices/{office.Identifier.Value}", office.ToResponse()),
            error => error.ToHttpResult());
    }

    private static async Task<IResult> ListOfficesAsync(IOfficeRepository offices, CancellationToken cancellationToken)
    {
        var all = await offices.GetAllAsync(cancellationToken);
        return Results.Ok(all.Select(office => office.ToResponse()));
    }

    private static async Task<IResult> GetOfficeAsync(Guid officeId, IOfficeRepository offices, CancellationToken cancellationToken)
    {
        var office = await offices.GetByIdentifierAsync(OfficeIdentifier.From(officeId), cancellationToken);
        return office.Match(found => Results.Ok(found.ToResponse()), error => error.ToHttpResult());
    }

    private static async Task<IResult> RenameOfficeAsync(
        Guid officeId,
        Request.RenameOffice request,
        ICommandHandler<RenameOffice> commandHandler,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var identifier = OfficeIdentifier.From(officeId);
        var command = new RenameOffice(
            identifier,
            OfficeName.From(request.Name));
        var result = await commandHandler.HandleAsync(command, cancellationToken);
        return await OfficeResultAsync(result, identifier, offices, cancellationToken);
    }

    private static async Task<IResult> ChangeLocationAsync(
        Guid officeId,
        Request.RelocateOffice request,
        ICommandHandler<ChangeOfficeLocation> commandHandler,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var officeIdentifier = OfficeIdentifier.From(officeId);
        var command = new ChangeOfficeLocation(
            officeIdentifier,
            Location.From(request.Location));
        var result = await commandHandler.HandleAsync(command, cancellationToken);
        return await OfficeResultAsync(result, officeIdentifier, offices, cancellationToken);
    }

    private static async Task<IResult> AddRoomAsync(
        Guid officeId,
        Request.AddRoom request,
        ICommandHandler<AddRoomToOffice, RoomIdentifier> commandHandler,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var officeIdentifier = OfficeIdentifier.From(officeId);
        var command = new AddRoomToOffice(
            officeIdentifier,
            RoomName.From(request.Name),
            Capacity.From(request.Capacity));
        var result = await commandHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return result.Error.ToHttpResult();

        var office = await offices.GetByIdentifierAsync(officeIdentifier, cancellationToken);
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
        ICommandHandler<RenameRoom> commandHandler,
        IOfficeRepository offices,
        CancellationToken cancellationToken)
    {
        var officeIdentifier = OfficeIdentifier.From(officeId);
        var command = new RenameRoom(
            officeIdentifier,
            RoomIdentifier.From(roomId),
            RoomName.From(request.Name));
        var result = await commandHandler.HandleAsync(command, cancellationToken);
        return await OfficeResultAsync(result, officeIdentifier, offices, cancellationToken);
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
