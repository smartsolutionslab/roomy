using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The "my reservations" use case is a straight read of the read model (FR-004): the handler returns
// exactly what the port holds, wrapped in a success Result, with no decision of its own. The SQL — the
// employee filter, the name joins, and the ordering — is covered by the read-model integration tests.
public class ViewMyReservationsTests
{
    [Fact]
    public async Task It_returns_the_read_models_reservations_for_the_employee()
    {
        var employee = EmployeeIdentifier.New();
        var reservation = new MyReservationView(
            ReservationIdentifier.New(),
            OfficeIdentifier.New(),
            "Munich",
            RoomIdentifier.New(),
            "A1",
            BookingDate.From(new DateOnly(2026, 6, 8)));
        var handler = new ViewMyReservationsHandler(new StubReadModel(employee, [reservation]));

        var result = await handler.HandleAsync(
            new ViewMyReservations(employee, FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldHaveSingleItem().ShouldBe(reservation);
    }

    [Fact]
    public async Task An_employee_with_no_reservations_gets_an_empty_page()
    {
        var employee = EmployeeIdentifier.New();
        var handler = new ViewMyReservationsHandler(new StubReadModel(employee, []));

        var result = await handler.HandleAsync(
            new ViewMyReservations(employee, FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null).Value;

    private sealed class StubReadModel(EmployeeIdentifier expected, IReadOnlyList<MyReservationView> reservations)
        : IMyReservationsReadModel
    {
        public Task<Result<Page<MyReservationView>>> GetAsync(
            EmployeeIdentifier employee, PageRequest request, CancellationToken cancellationToken)
        {
            employee.ShouldBe(expected);
            return Task.FromResult<Result<Page<MyReservationView>>>(
                new Page<MyReservationView>(reservations, null));
        }
    }
}
