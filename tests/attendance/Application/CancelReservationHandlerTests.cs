using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

public class CancelReservationHandlerTests
{
    private static readonly DateOnly mondayDate = BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1));
    private static readonly DateTimeOffset now = new(mondayDate.Year, mondayDate.Month, mondayDate.Day, 8, 0, 0, TimeSpan.Zero);
    private static readonly BookingDate bookingDate = BookingDate.From(mondayDate);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public async Task Cancelling_a_held_reservation_applies_the_cancellation()
    {
        var reservationId = ReservationIdentifier.New();
        var owner = EmployeeIdentifier.New();
        var repository = RepositoryApplyingAgainst(SeededDay(reservationId, owner));
        var handler = new CancelReservationHandler(repository, ClockAt(now, bookingDate));

        var result = await handler.HandleAsync(
            new CancelReservation(company, reservationId, bookingDate, owner, ActorIsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await repository.Received(1).MutateAsync(
            Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(),
            Arg.Any<Func<AttendanceDay, Result>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_reservation_is_rejected()
    {
        var repository = RepositoryApplyingAgainst(AttendanceDay.For(company, bookingDate));
        var handler = new CancelReservationHandler(repository, ClockAt(now, bookingDate));

        var result = await handler.HandleAsync(
            new CancelReservation(company, ReservationIdentifier.New(), bookingDate, EmployeeIdentifier.New(), ActorIsAdmin: false),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("reservation_not_found");
    }

    private static IAttendanceDayRepository RepositoryApplyingAgainst(AttendanceDay day)
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.MutateAsync(
            Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(),
            Arg.Any<Func<AttendanceDay, Result>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<Func<AttendanceDay, Result>>()(day)));
        return repository;
    }

    private static IBusinessClock ClockAt(DateTimeOffset instant, BookingDate today)
    {
        var clock = Substitute.For<IBusinessClock>();
        clock.Now.Returns(instant);
        clock.Today.Returns(today);
        return clock;
    }

    private static AttendanceDay SeededDay(ReservationIdentifier reservation, EmployeeIdentifier owner)
    {
        var day = AttendanceDay.For(company, bookingDate);
        day.LoadFromHistory(
        [
            new ReservationPlaced(
                reservation.Value,
                company.Value,
                bookingDate.Value,
                owner.Value,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                now)
        ]);
        return day;
    }
}
