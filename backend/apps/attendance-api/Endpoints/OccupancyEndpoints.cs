using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

public static class OccupancyEndpoints
{
    private const int MaxRangeDays = 31;

    public static IEndpointRouteBuilder MapOccupancyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/occupancy", ViewAsync)
            .RequireAuthorization()
            .WithName("ViewOccupancy")
            .Produces<IEnumerable<Response.OccupancyDay>>()
            .ProducesError(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static async Task<IResult> ViewAsync(
        Guid? officeId,
        Guid? roomId,
        DateOnly? from,
        DateOnly? to,
        AttendanceApiOptions options,
        IBusinessClock clock,
        IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>> queryHandler,
        CancellationToken cancellationToken)
    {
        if (officeId is null == roomId is null) throw new ArgumentException("Provide exactly one of officeId or roomId.");

        var range = ResolveRange(from, to, clock);

        var scope = officeId is { } office
            ? OccupancyScope.ForOffice(OfficeIdentifier.From(office))
            : OccupancyScope.ForRoom(RoomIdentifier.From(roomId!.Value));

        var query = new ViewOccupancy(
            CompanyIdentifier.From(options.CompanyId),
            scope,
            range);
        var result = await queryHandler.HandleAsync(query, cancellationToken);

        return result.ToOk(days => days.Select(day => day.ToResponse()));
    }

    // Resolves the optional from/to query params into a validated range: defaults to today, defaults an
    // open end to the start, and caps the span at MaxRangeDays.
    private static BookingDateRange ResolveRange(DateOnly? from, DateOnly? to, IBusinessClock clock)
    {
        var today = clock.Today.Value;
        var rangeFrom = from ?? today;
        var rangeTo = to ?? rangeFrom;

        if (BookingDateRange.TryParse(rangeFrom, rangeTo) is not { } range || range.LengthInDays > MaxRangeDays)
        {
            throw new ArgumentOutOfRangeException(nameof(to), $"The range must be a non-empty span of at most {MaxRangeDays} days.");
        }

        return range;
    }
}
