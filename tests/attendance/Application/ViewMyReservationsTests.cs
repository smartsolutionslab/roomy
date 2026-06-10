using NSubstitute;
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
        var readModel = Substitute.For<IMyReservationsReadModel>();
        readModel.GetAsync(Arg.Any<EmployeeIdentifier>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new Page<MyReservationView>([reservation], null)));
        var handler = new ViewMyReservationsHandler(readModel);

        var result = await handler.HandleAsync(
            new ViewMyReservations(employee, FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldHaveSingleItem().ShouldBe(reservation);
        await readModel.Received(1).GetAsync(employee, Arg.Any<PageRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_employee_with_no_reservations_gets_an_empty_page()
    {
        var employee = EmployeeIdentifier.New();
        var readModel = Substitute.For<IMyReservationsReadModel>();
        readModel.GetAsync(Arg.Any<EmployeeIdentifier>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new Page<MyReservationView>([], null)));
        var handler = new ViewMyReservationsHandler(readModel);

        var result = await handler.HandleAsync(
            new ViewMyReservations(employee, FirstPage), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null).Value;
}
