using System.Security.Claims;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

// The reservation surface (contract: attendance-api.md). The service is internal — reached only through
// the YARP BFF, which forwards the Keycloak access token — so the caller is the token's subject, resolved
// to its EmployeeId via the Employees read model (003 US4). An administrator (realm role) may act on
// behalf of anyone (FR-011); an employee acts only on their own reservation (FR-012). The booking rules
// live in the aggregate and handler; this maps HTTP to the command and the Result to a status code.
public static class ReservationEndpoints
{
    private const string AdministratorRole = "administrator";

    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/reservations", ReserveAsync).RequireAuthorization();
        endpoints.MapDelete("/reservations/{reservationId:guid}", CancelAsync).RequireAuthorization();
        endpoints.MapGet("/reservations", ViewAsync).RequireAuthorization();
        return endpoints;
    }

    // GET /reservations?date=YYYY-MM-DD — view the company-day's reservations (FR-012, scenario 11).
    // Any authenticated employee may view; the result is replayed from the AttendanceDay stream.
    private static async Task<IResult> ViewAsync(
        DateOnly date,
        AttendanceApiOptions options,
        IQueryHandler<ViewDayReservations, IReadOnlyList<ReservationView>> view,
        CancellationToken cancellationToken)
    {
        var query = new ViewDayReservations(
            CompanyIdentifier.From(options.CompanyId),
            BookingDate.From(date));

        var result = await view.HandleAsync(query, cancellationToken);

        return result.Match(
            reservations => Results.Ok(reservations.Select(reservation => new ReservationResponse(
                reservation.Reservation.Value,
                reservation.Office.Value,
                reservation.Room.Value,
                reservation.Date.Value,
                reservation.Employee.Value))),
            error => error.ToHttpResult());
    }

    // POST /reservations — reserve a place in a room for a day (FR-001/011). The acting employee is
    // resolved from the token; onBehalfOf is administrator-only. The Result maps to 201/409/422/404/403.
    private static async Task<IResult> ReserveAsync(
        ReserveRequest request,
        ClaimsPrincipal principal,
        AttendanceApiOptions options,
        IEmployeeDirectory employees,
        ICommandHandler<ReservePlace, ReservationIdentifier> reserve,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubject(principal, out var subject))
        {
            return Results.Unauthorized();
        }

        var actor = await employees.FindByUserAsync(UserIdentifier.From(subject), cancellationToken);
        if (actor.IsFailure)
        {
            return actor.Error.ToHttpResult();
        }

        var employee = request.OnBehalfOf is { } onBehalfOf
            ? EmployeeIdentifier.From(onBehalfOf)
            : actor.Value;

        if (employee != actor.Value && !principal.IsInRole(AdministratorRole))
        {
            return Error.Forbidden(
                "not_authorized",
                "Only an administrator may reserve on behalf of another employee.").ToHttpResult();
        }

        var command = new ReservePlace(
            CompanyIdentifier.From(options.CompanyId),
            employee,
            OfficeIdentifier.From(request.OfficeId),
            RoomIdentifier.From(request.RoomId),
            BookingDate.From(request.Date));

        var result = await reserve.HandleAsync(command, cancellationToken);

        return result.Match(
            reservationId => Results.Created(
                $"/reservations/{reservationId.Value}",
                new ReservationResponse(reservationId.Value, request.OfficeId, request.RoomId, request.Date, employee.Value)),
            error => error.ToHttpResult());
    }

    // DELETE /reservations/{reservationId}?date=YYYY-MM-DD — cancel a reservation, freeing the place
    // (FR-008). The date locates the company-day stream (the id alone cannot). The owner or an
    // administrator may cancel (FR-012); the Result maps to 204 / 404 / 422 / 403.
    private static async Task<IResult> CancelAsync(
        Guid reservationId,
        DateOnly date,
        ClaimsPrincipal principal,
        AttendanceApiOptions options,
        IEmployeeDirectory employees,
        ICommandHandler<CancelReservation> cancel,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubject(principal, out var subject))
        {
            return Results.Unauthorized();
        }

        var actor = await employees.FindByUserAsync(UserIdentifier.From(subject), cancellationToken);
        if (actor.IsFailure)
        {
            return actor.Error.ToHttpResult();
        }

        var command = new CancelReservation(
            CompanyIdentifier.From(options.CompanyId),
            ReservationIdentifier.From(reservationId),
            BookingDate.From(date),
            actor.Value,
            ActorIsAdmin: principal.IsInRole(AdministratorRole));

        var result = await cancel.HandleAsync(command, cancellationToken);

        return result.Match(Results.NoContent, error => error.ToHttpResult());
    }

    private static bool TryGetSubject(ClaimsPrincipal principal, out Guid subject)
    {
        var subjectClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(subjectClaim, out subject);
    }
}

internal sealed record ReserveRequest(Guid OfficeId, Guid RoomId, DateOnly Date, Guid? OnBehalfOf = null);

internal sealed record ReservationResponse(Guid ReservationId, Guid OfficeId, Guid RoomId, DateOnly Date, Guid EmployeeId);
