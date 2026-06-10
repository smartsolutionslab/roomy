using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The cancel use case: load the company-day, cancel, save — inside the same bounded optimistic-retry
// as reserve (a concurrent write to the day reloads and re-decides). A domain rejection (not found,
// past, not owner) returns at once. The day is located by (company, date) from the command — the
// reservation id alone can't address the stream (D: date carried in the request).
public class CancelReservationHandlerTests
{
    private static readonly DateOnly mondayDate = BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1));
    private static readonly DateTimeOffset now = new(mondayDate.Year, mondayDate.Month, mondayDate.Day, 8, 0, 0, TimeSpan.Zero);
    private static readonly BookingDate bookingDate = BookingDate.From(mondayDate);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public async Task Cancelling_a_held_reservation_succeeds_and_saves_once()
    {
        var reservationId = ReservationIdentifier.New();
        var owner = EmployeeIdentifier.New();
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => SeededDay(reservationId, owner));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, reservationId, bookingDate, owner, ActorIsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await repository.Received(1).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_reservation_is_rejected_without_saving()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate));
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, ReservationIdentifier.New(), bookingDate, EmployeeIdentifier.New(), ActorIsAdmin: false),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("reservation_not_found");
        await repository.DidNotReceive().SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_concurrency_conflict_is_retried_and_can_then_succeed()
    {
        var reservationId = ReservationIdentifier.New();
        var owner = EmployeeIdentifier.New();
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => SeededDay(reservationId, owner));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(ConcurrencyConflict(), Result.Success());
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, reservationId, bookingDate, owner, ActorIsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await repository.Received(2).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.Received(2).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Exhausting_the_retries_returns_a_retryable_conflict()
    {
        var reservationId = ReservationIdentifier.New();
        var owner = EmployeeIdentifier.New();
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => SeededDay(reservationId, owner));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(ConcurrencyConflict());
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, reservationId, bookingDate, owner, ActorIsAdmin: false),
            CancellationToken.None);

        result.Error.Code.ShouldBe("concurrency_retry_exhausted");
        await repository.Received(3).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    private static AttendanceDay SeededDay(ReservationIdentifier reservation, EmployeeIdentifier owner)
    {
        var day = AttendanceDay.For(company, bookingDate);
        day.LoadFromHistory(
        [
            new ReservationPlaced(reservation.Value, company.Value, bookingDate.Value, owner.Value, Guid.CreateVersion7(), Guid.CreateVersion7(), now),
        ]);
        return day;
    }

    private static Result ConcurrencyConflict() =>
        Error.Conflict("concurrency_conflict", "The day changed concurrently.");
}
