using System.Security.Claims;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;
using SmartSolutionsLab.Roomy.Web.Http;

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
        endpoints.MapPost("/reservations", ReserveAsync)
            .RequireAuthorization()
            .WithName("Reserve")
            .Produces<ReservationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);
        endpoints.MapDelete("/reservations/{reservationId:guid}", CancelAsync)
            .RequireAuthorization()
            .WithName("CancelReservation")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);
        endpoints.MapGet("/reservations", ViewAsync)
            .RequireAuthorization()
            .WithName("ViewDayReservations")
            .Produces<ReservationPage>();
        endpoints.MapGet("/reservations/mine", ViewMineAsync)
            .RequireAuthorization()
            .WithName("ViewMyReservations")
            .Produces<MyReservationPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        endpoints.MapGet("/reservations/employees", ViewEmployeesAsync)
            .RequireAuthorization()
            .WithName("ViewEmployees")
            .Produces<EmployeePage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        endpoints.MapGet("/reservations/by-employee/{employeeId:guid}", ViewForEmployeeAsync)
            .RequireAuthorization()
            .WithName("ViewReservationsForEmployee")
            .Produces<MyReservationPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
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

        // The company-day is bounded by room capacity and replayed from the aggregate in memory, so it
        // is one page — it adopts the page envelope for contract uniformity with nextCursor always null
        // (ADR-0044), never keyset-paginated.
        return result.Match(
            reservations => Results.Ok(new ReservationPage(
                reservations.Select(reservation => new ReservationResponse(
                    reservation.Reservation.Value,
                    reservation.Office.Value,
                    reservation.Room.Value,
                    reservation.Date.Value,
                    reservation.Employee.Value)).ToList(),
                NextCursor: null)),
            error => error.ToHttpResult());
    }

    // GET /reservations/mine — the caller's own reservations, past and future (FR-004, scenario 6). The
    // acting employee is resolved from the token; past reservations are returned as history and the client
    // offers cancellation only for future ones (the rule lives in the cancel endpoint).
    private static async Task<IResult> ViewMineAsync(
        string? cursor,
        int? limit,
        ClaimsPrincipal principal,
        IEmployeeDirectory employees,
        IQueryHandler<ViewMyReservations, Page<MyReservationView>> view,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubject(principal, out var subject))
        {
            return Results.Unauthorized();
        }

        var pageRequest = PageRequest.From(cursor, limit);
        if (pageRequest.IsFailure)
        {
            return BadRequest(pageRequest.Error);
        }

        var actor = await employees.FindByUserAsync(UserIdentifier.From(subject), cancellationToken);
        if (actor.IsFailure)
        {
            return actor.Error.ToHttpResult();
        }

        var result = await view.HandleAsync(
            new ViewMyReservations(actor.Value, pageRequest.Value), cancellationToken);

        return result.Match(MyReservationPageResult, BadRequest);
    }

    // GET /reservations/employees — the directory an administrator picks from to act on behalf (009,
    // AT-6). Administrator-only on the server (FR-009), not merely UI-hidden. An optional q searches by name
    // similarity, best match first; a blank q lists in the existing keyset order (012, ADR-0047). An over-long
    // q is rejected here as a 400 before any query runs.
    private static async Task<IResult> ViewEmployeesAsync(
        string? q,
        string? cursor,
        int? limit,
        ClaimsPrincipal principal,
        IQueryHandler<ViewEmployees, Page<EmployeeView>> view,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole(AdministratorRole))
        {
            return Error.Forbidden("not_authorized", "Only an administrator may list employees.").ToHttpResult();
        }

        var searchTerm = SearchTerm.From(q);
        if (searchTerm.IsFailure)
        {
            return BadRequest(searchTerm.Error);
        }

        var pageRequest = PageRequest.From(cursor, limit);
        if (pageRequest.IsFailure)
        {
            return BadRequest(pageRequest.Error);
        }

        var result = await view.HandleAsync(new ViewEmployees(searchTerm.Value, pageRequest.Value), cancellationToken);

        return result.Match(
            employees => Results.Ok(new EmployeePage(
                employees.Items.Select(employee => new EmployeeResponse(employee.Employee.Value, employee.Name)).ToList(),
                employees.NextCursor)),
            BadRequest);
    }

    // GET /reservations/by-employee/{employeeId} — a chosen employee's reservations, for the administrator
    // on-behalf view (009). Administrator-only; reuses the "my reservations" query for the target employee.
    private static async Task<IResult> ViewForEmployeeAsync(
        Guid employeeId,
        string? cursor,
        int? limit,
        ClaimsPrincipal principal,
        IQueryHandler<ViewMyReservations, Page<MyReservationView>> view,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole(AdministratorRole))
        {
            return Error.Forbidden("not_authorized", "Only an administrator may view another employee's reservations.").ToHttpResult();
        }

        var pageRequest = PageRequest.From(cursor, limit);
        if (pageRequest.IsFailure)
        {
            return BadRequest(pageRequest.Error);
        }

        var result = await view.HandleAsync(
            new ViewMyReservations(EmployeeIdentifier.From(employeeId), pageRequest.Value), cancellationToken);

        return result.Match(MyReservationPageResult, BadRequest);
    }

    private static IResult MyReservationPageResult(Page<MyReservationView> page) =>
        Results.Ok(new MyReservationPage(
            page.Items.Select(reservation => new MyReservationResponse(
                reservation.Reservation.Value,
                reservation.Office.Value,
                reservation.OfficeName,
                reservation.Room.Value,
                reservation.RoomName,
                reservation.Date.Value)).ToList(),
            page.NextCursor));

    // A bad limit or a malformed cursor is request validation, not a domain rule — a 400, distinct from
    // the 422 the domain Validation errors map to via ToHttpResult (ADR-0044).
    private static IResult BadRequest(Error error) =>
        Results.Json(new ErrorResponse(error.Code, error.Message), statusCode: StatusCodes.Status400BadRequest);

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

        if (!MayReserveFor(employee, actor.Value, principal))
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

    // An employee may reserve for themselves; reserving for anyone else is administrator-only (FR-011).
    private static bool MayReserveFor(EmployeeIdentifier employee, EmployeeIdentifier actor, ClaimsPrincipal principal) =>
        employee == actor || principal.IsInRole(AdministratorRole);

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

