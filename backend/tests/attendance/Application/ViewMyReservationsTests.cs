using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

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
            .Returns(new Page<MyReservationView>([reservation], null));
        var handler = new ViewMyReservationsHandler(readModel);

        var result = await handler.HandleAsync(
            new ViewMyReservations(employee, FirstPage), TestContext.Current.CancellationToken);

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
            .Returns(new Page<MyReservationView>([], null));
        var handler = new ViewMyReservationsHandler(readModel);

        var result = await handler.HandleAsync(
            new ViewMyReservations(employee, FirstPage), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null);
}
