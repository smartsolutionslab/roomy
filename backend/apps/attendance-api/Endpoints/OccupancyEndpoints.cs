using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
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
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);
        return endpoints;
    }

    private static async Task<IResult> ViewAsync(
        Guid? officeId,
        Guid? roomId,
        DateOnly? from,
        DateOnly? to,
        AttendanceApiOptions options,
        IBusinessClock clock,
        IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>> view,
        CancellationToken cancellationToken)
    {
        if (officeId is null == roomId is null) return Error.Validation("unknown_scope", "Provide exactly one of officeId or roomId.").ToHttpResult();

        var range = ResolveRange(from, to, clock);
        if (range.IsFailure) return range.Error.ToHttpResult();

        var scope = officeId is { } office
            ? OccupancyScope.ForOffice(OfficeIdentifier.From(office))
            : OccupancyScope.ForRoom(RoomIdentifier.From(roomId!.Value));

        var query = new ViewOccupancy(CompanyIdentifier.From(options.CompanyId), scope, range.Value);

        var result = await view.HandleAsync(query, cancellationToken);

        return result.Match(
            days => Results.Ok(days.Select(day => day.ToResponse())),
            error => error.ToHttpResult());
    }

    // Resolves the optional from/to query params into a validated range: defaults to today, defaults an
    // open end to the start, and caps the span at MaxRangeDays.
    private static Result<BookingDateRange> ResolveRange(DateOnly? from, DateOnly? to, IBusinessClock clock)
    {
        var today = clock.Today.Value;
        var rangeFrom = from ?? today;
        var rangeTo = to ?? rangeFrom;

        if (BookingDateRange.TryParse(rangeFrom, rangeTo) is not { } range || range.LengthInDays > MaxRangeDays)
        {
            return Error.Validation("range_too_large", $"The range must be a non-empty span of at most {MaxRangeDays} days.");
        }

        return range;
    }
}
