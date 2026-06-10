using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

// The bookable catalogue surface (007 US1). The service is internal — reached only through the YARP BFF
// — but any authenticated employee may book, so there is no owner/admin check. Organization owns
// /offices at the gateway, so attendance exposes its catalogue at /rooms (the rooms you can book, each
// carrying its office). It reads attendance's own Offices/Rooms read models — never a cross-service join.
public static class RoomCatalogueEndpoints
{
    public static IEndpointRouteBuilder MapRoomCatalogueEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/rooms", ListAsync)
            .RequireAuthorization()
            .WithName("ViewBookableRooms")
            .Produces<IEnumerable<Response.BookableRoom>>();
        return endpoints;
    }

    // GET /rooms — every bookable room with its office, both named, and the room's capacity (007 US1).
    // The single-tenant company scopes the catalogue; the client groups the flat list by office.
    private static async Task<IResult> ListAsync(
        AttendanceApiOptions options,
        IQueryHandler<ViewBookableRooms, IReadOnlyList<BookableRoomView>> view,
        CancellationToken cancellationToken)
    {
        var query = new ViewBookableRooms(CompanyIdentifier.From(options.CompanyId));

        var result = await view.HandleAsync(query, cancellationToken);

        return result.Match(
            rooms => Results.Ok(rooms.Select(room => new Response.BookableRoom(
                room.Office.Value,
                room.OfficeName,
                room.Room.Value,
                room.RoomName,
                room.Capacity.Value))),
            error => error.ToHttpResult());
    }
}
