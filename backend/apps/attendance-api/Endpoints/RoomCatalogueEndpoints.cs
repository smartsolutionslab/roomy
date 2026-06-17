using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

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

    private static async Task<IResult> ListAsync(
        AttendanceApiOptions options,
        IQueryHandler<ViewBookableRooms, IReadOnlyList<BookableRoomView>> queryHandler,
        CancellationToken cancellationToken)
    {
        var query = new ViewBookableRooms(CompanyIdentifier.From(options.CompanyId));
        var result = await queryHandler.HandleAsync(query, cancellationToken);

        return result.ToOk(rooms => rooms.Select(room => room.ToResponse()));
    }
}
