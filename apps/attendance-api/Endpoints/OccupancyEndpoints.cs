using System.Text.Json.Serialization;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.Web.Http;

namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

// The occupancy read surface (contract: attendance-api.md, 004 US6). The service is internal — reached
// only through the YARP BFF, which forwards the Keycloak token — but any authenticated user may view any
// office or room (FR-005), so there is no owner/admin check. The view is read-only (FR-006); the figures
// and the today/tomorrow name policy (FR-007) are computed by the query handler. This maps HTTP to the
// query and the Result to a status code.
public static class OccupancyEndpoints
{
    private const int MaxRangeDays = 31;

    private static readonly TimeZoneInfo berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static IEndpointRouteBuilder MapOccupancyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/occupancy", ViewAsync)
            .RequireAuthorization()
            .WithName("ViewOccupancy")
            .Produces<IEnumerable<OccupancyDayResponse>>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);
        return endpoints;
    }

    // GET /occupancy?officeId=…|roomId=…&from=YYYY-MM-DD&to=YYYY-MM-DD — per-room occupancy + office
    // rollup for each day in the range (FR-001/002, scenarios 1–4, 7–9). Exactly one of officeId/roomId is
    // required; the range defaults to today and is bounded; past days are allowed (FR-009).
    private static async Task<IResult> ViewAsync(
        Guid? officeId,
        Guid? roomId,
        DateOnly? from,
        DateOnly? to,
        AttendanceApiOptions options,
        TimeProvider timeProvider,
        IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>> view,
        CancellationToken cancellationToken)
    {
        if (officeId is null == roomId is null)
        {
            return Error.Validation("unknown_scope", "Provide exactly one of officeId or roomId.").ToHttpResult();
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), berlinZone).DateTime);
        var rangeFrom = from ?? today;
        var rangeTo = to ?? rangeFrom;

        if (rangeTo < rangeFrom || rangeTo.DayNumber - rangeFrom.DayNumber + 1 > MaxRangeDays)
        {
            return Error.Validation(
                "range_too_large",
                $"The range must be a non-empty span of at most {MaxRangeDays} days.").ToHttpResult();
        }

        var scope = officeId is { } office
            ? OccupancyScope.ForOffice(OfficeIdentifier.From(office))
            : OccupancyScope.ForRoom(RoomIdentifier.From(roomId!.Value));

        var query = new ViewOccupancy(
            CompanyIdentifier.From(options.CompanyId),
            scope,
            BookingDate.From(rangeFrom),
            BookingDate.From(rangeTo));

        var result = await view.HandleAsync(query, cancellationToken);

        return result.Match(
            days => Results.Ok(days.Select(ToResponse)),
            error => error.ToHttpResult());
    }

    private static OccupancyDayResponse ToResponse(OccupancyView day) =>
        new(
            day.Date.Value,
            new OfficeOccupancyResponse(
                day.Office.Office.Value,
                day.Office.Name,
                day.Office.Occupied,
                day.Office.Capacity,
                day.Office.IsFull),
            day.Rooms.Select(room => new RoomOccupancyResponse(
                room.Room.Value,
                room.Name,
                room.Occupied,
                room.Capacity,
                room.IsFull,
                room.Occupants?
                    .Select(occupant => new OccupantResponse(occupant.Employee.Value, occupant.Name))
                    .ToList())).ToList());
}

internal sealed record OccupancyDayResponse(
    DateOnly Date,
    OfficeOccupancyResponse Office,
    IReadOnlyList<RoomOccupancyResponse> Rooms);

internal sealed record OfficeOccupancyResponse(Guid OfficeId, string Name, int Occupied, int Capacity, bool IsFull);

internal sealed record RoomOccupancyResponse(
    Guid RoomId,
    string Name,
    int Occupied,
    int Capacity,
    bool IsFull,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<OccupantResponse>? Occupants);

internal sealed record OccupantResponse(Guid EmployeeId, string Name);
