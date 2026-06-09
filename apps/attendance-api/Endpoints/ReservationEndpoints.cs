using System.Security.Claims;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

// The reservation surface (contract: attendance-api.md). The service is internal — reached only through
// the YARP BFF, which forwards the Keycloak access token — so the caller is identified by the token's
// subject. The booking rules and outcomes live in the aggregate and handler; this maps HTTP to the
// command and the Result to a status code.
public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/reservations", ReserveAsync).RequireAuthorization();
        endpoints.MapDelete("/reservations/{reservationId:guid}", CancelAsync).RequireAuthorization();
        return endpoints;
    }

    // POST /reservations — reserve a place in a room for a day (FR-001). 401 is enforced by the
    // authorization policy; the Result maps to 201 / 409 / 422 / 404 per the contract.
    private static async Task<IResult> ReserveAsync(
        ReserveRequest request,
        ClaimsPrincipal principal,
        AttendanceApiOptions options,
        ICommandHandler<ReservePlace, ReservationIdentifier> reserve,
        CancellationToken cancellationToken)
    {
        var subjectClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (subjectClaim is null || !Guid.TryParse(subjectClaim, out var subject))
        {
            return Results.Unauthorized();
        }

        // US1: the authenticated subject stands in as the acting employee. US4 resolves the
        // UserId -> EmployeeId mapping via the Employees read model and adds admin on-behalf-of.
        var command = new ReservePlace(
            CompanyIdentifier.From(options.CompanyId),
            EmployeeIdentifier.From(subject),
            OfficeIdentifier.From(request.OfficeId),
            RoomIdentifier.From(request.RoomId),
            BookingDate.From(request.Date));

        var result = await reserve.HandleAsync(command, cancellationToken);

        return result.Match(
            reservationId => Results.Created(
                $"/reservations/{reservationId.Value}",
                new ReservationResponse(reservationId.Value, request.OfficeId, request.RoomId, request.Date, subject)),
            error => error.ToHttpResult());
    }

    // DELETE /reservations/{reservationId}?date=YYYY-MM-DD — cancel a reservation, freeing the place
    // (FR-008). The date locates the company-day stream (the id alone cannot). 204 on success; the
    // Result maps to 404 / 422 / 403 per the contract.
    private static async Task<IResult> CancelAsync(
        Guid reservationId,
        DateOnly date,
        ClaimsPrincipal principal,
        AttendanceApiOptions options,
        ICommandHandler<CancelReservation> cancel,
        CancellationToken cancellationToken)
    {
        var subjectClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (subjectClaim is null || !Guid.TryParse(subjectClaim, out var subject))
        {
            return Results.Unauthorized();
        }

        // US1/US3: the authenticated subject stands in as the acting employee and is never an admin.
        // US4 resolves the UserId -> EmployeeId mapping and the administrator role from the token.
        var command = new CancelReservation(
            CompanyIdentifier.From(options.CompanyId),
            ReservationIdentifier.From(reservationId),
            BookingDate.From(date),
            EmployeeIdentifier.From(subject),
            ActorIsAdmin: false);

        var result = await cancel.HandleAsync(command, cancellationToken);

        return result.Match(Results.NoContent, error => error.ToHttpResult());
    }
}

internal sealed record ReserveRequest(Guid OfficeId, Guid RoomId, DateOnly Date);

internal sealed record ReservationResponse(Guid ReservationId, Guid OfficeId, Guid RoomId, DateOnly Date, Guid EmployeeId);
