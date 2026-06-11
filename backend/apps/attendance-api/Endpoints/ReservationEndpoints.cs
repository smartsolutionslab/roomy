using System.Security.Claims;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/reservations", ReserveAsync)
            .RequireAuthorization()
            .WithName("Reserve")
            .Produces<Response.Reservation>(StatusCodes.Status201Created)
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
            .Produces<Response.Page.Reservation>();
        endpoints.MapGet("/reservations/mine", ViewMineAsync)
            .RequireAuthorization()
            .WithName("ViewMyReservations")
            .Produces<Response.Page.MyReservation>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        endpoints.MapGet("/reservations/employees", ViewEmployeesAsync)
            .RequireAdministrator()
            .WithName("ViewEmployees")
            .Produces<Response.Page.Employee>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        endpoints.MapGet("/reservations/by-employee/{employeeId:guid}", ViewForEmployeeAsync)
            .RequireAdministrator()
            .WithName("ViewReservationsForEmployee")
            .Produces<Response.Page.MyReservation>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        return endpoints;
    }

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
            reservations => Results.Ok(reservations.ToResponse()),
            error => error.ToHttpResult());
    }

    private static async Task<IResult> ViewMineAsync(
        string? cursor,
        int? limit,
        ClaimsPrincipal principal,
        IEmployeeDirectory employees,
        IQueryHandler<ViewMyReservations, Page<MyReservationView>> view,
        CancellationToken cancellationToken)
    {
        var userId = principal.UserId();
        if (userId.IsFailure) return Results.Unauthorized();

        var pageRequest = PageRequest.From(cursor, limit);
        if (pageRequest.IsFailure) return BadRequest(pageRequest.Error);

        var actor = await employees.FindByUserAsync(UserIdentifier.From(userId.Value), cancellationToken);
        if (actor.IsFailure) return actor.Error.ToHttpResult();

        var result = await view.HandleAsync(new ViewMyReservations(actor.Value, pageRequest.Value), cancellationToken);

        return result.Match(page => Results.Ok(page.ToResponse()), BadRequest);
    }

    private static async Task<IResult> ViewEmployeesAsync(
        string? q,
        string? cursor,
        int? limit,
        IQueryHandler<ViewEmployees, Page<EmployeeView>> view,
        CancellationToken cancellationToken)
    {
        var searchTerm = SearchTerm.From(q);
        if (searchTerm.IsFailure) return BadRequest(searchTerm.Error);

        var pageRequest = PageRequest.From(cursor, limit);
        if (pageRequest.IsFailure) return BadRequest(pageRequest.Error);

        var result = await view.HandleAsync(new ViewEmployees(searchTerm.Value, pageRequest.Value), cancellationToken);

        return result.Match(employees => Results.Ok(employees.ToResponse()), BadRequest);
    }

    private static async Task<IResult> ViewForEmployeeAsync(
        Guid employeeId,
        string? cursor,
        int? limit,
        IQueryHandler<ViewMyReservations, Page<MyReservationView>> view,
        CancellationToken cancellationToken)
    {
        var pageRequest = PageRequest.From(cursor, limit);
        if (pageRequest.IsFailure) return BadRequest(pageRequest.Error);

        var result = await view.HandleAsync(new ViewMyReservations(EmployeeIdentifier.From(employeeId), pageRequest.Value), cancellationToken);

        return result.Match(page => Results.Ok(page.ToResponse()), BadRequest);
    }

    private static IResult BadRequest(Error error) =>
        Results.Json(new ErrorResponse(error.Code, error.Message), statusCode: StatusCodes.Status400BadRequest);

    private static async Task<IResult> ReserveAsync(
        Request.Reserve request,
        ClaimsPrincipal principal,
        AttendanceApiOptions options,
        IEmployeeDirectory employees,
        ICommandHandler<ReservePlace, ReservationIdentifier> reserve,
        CancellationToken cancellationToken)
    {
        var userId = principal.UserId();
        if (userId.IsFailure) return Results.Unauthorized();

        var actor = await employees.FindByUserAsync(UserIdentifier.From(userId.Value), cancellationToken);
        if (actor.IsFailure) return actor.Error.ToHttpResult();

        var employee = request.OnBehalfOf is { } onBehalfOf
            ? EmployeeIdentifier.From(onBehalfOf)
            : actor.Value;

        var command = new ReservePlace(
            CompanyIdentifier.From(options.CompanyId),
            employee,
            actor.Value,
            OfficeIdentifier.From(request.OfficeId),
            RoomIdentifier.From(request.RoomId),
            BookingDate.From(request.Date),
            principal.IsAdministrator());

        var result = await reserve.HandleAsync(command, cancellationToken);

        return result.Match(
            reservationId => Results.Created(
                $"/reservations/{reservationId.Value}",
                new Response.Reservation(
                    reservationId.Value,
                    request.OfficeId,
                    request.RoomId,
                    request.Date,
                    employee.Value)),
            error => error.ToHttpResult());
    }

    private static async Task<IResult> CancelAsync(
        Guid reservationId,
        DateOnly date,
        ClaimsPrincipal principal,
        AttendanceApiOptions options,
        IEmployeeDirectory employees,
        ICommandHandler<CancelReservation> cancel,
        CancellationToken cancellationToken)
    {
        var userId = principal.UserId();
        if (userId.IsFailure) return Results.Unauthorized();

        var actor = await employees.FindByUserAsync(UserIdentifier.From(userId.Value), cancellationToken);
        if (actor.IsFailure) return actor.Error.ToHttpResult();

        var command = new CancelReservation(
            CompanyIdentifier.From(options.CompanyId),
            ReservationIdentifier.From(reservationId),
            BookingDate.From(date),
            actor.Value,
            ActorIsAdmin: principal.IsAdministrator());

        var result = await cancel.HandleAsync(command, cancellationToken);

        return result.Match(Results.NoContent, error => error.ToHttpResult());
    }
}
