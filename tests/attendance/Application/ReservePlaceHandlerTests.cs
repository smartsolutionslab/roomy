using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

public class ReservePlaceHandlerTests
{
    private static readonly DateOnly mondayDate = BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1));
    private static readonly DateTimeOffset now = new(mondayDate.Year, mondayDate.Month, mondayDate.Day, 8, 0, 0, TimeSpan.Zero);
    private static readonly BookingDate bookingDate = BookingDate.From(mondayDate);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    [Fact]
    public async Task Reserving_an_available_room_applies_the_reservation_and_returns_its_id()
    {
        var repository = RepositoryThatApplies();
        var handler = new ReservePlaceHandler(repository, RoomDirectoryWith(capacity: 8), ClockAt(now, bookingDate));

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldNotBe(Guid.Empty);
        await repository.Received(1).MutateAsync(
            Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(),
            Arg.Any<Func<AttendanceDay, Result<ReservationIdentifier>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_room_is_rejected_without_mutating_the_aggregate()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        var handler = new ReservePlaceHandler(repository, RoomDirectoryWith(capacity: null), ClockAt(now, bookingDate));

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_room");
        await repository.DidNotReceive().MutateAsync(
            Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(),
            Arg.Any<Func<AttendanceDay, Result<ReservationIdentifier>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_non_administrator_reserving_for_another_employee_is_forbidden_without_mutating()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        var handler = new ReservePlaceHandler(repository, RoomDirectoryWith(capacity: 8), ClockAt(now, bookingDate));
        var onBehalf = NewCommand() with { Employee = EmployeeIdentifier.New(), ActorIsAdmin = false };

        var result = await handler.HandleAsync(onBehalf, CancellationToken.None);

        result.Error.Code.ShouldBe("not_authorized");
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        await repository.DidNotReceive().MutateAsync(
            Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(),
            Arg.Any<Func<AttendanceDay, Result<ReservationIdentifier>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_administrator_may_reserve_for_another_employee()
    {
        var repository = RepositoryThatApplies();
        var handler = new ReservePlaceHandler(repository, RoomDirectoryWith(capacity: 8), ClockAt(now, bookingDate));
        var onBehalf = NewCommand() with { Employee = EmployeeIdentifier.New(), ActorIsAdmin = true };

        var result = await handler.HandleAsync(onBehalf, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await repository.Received(1).MutateAsync(
            Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(),
            Arg.Any<Func<AttendanceDay, Result<ReservationIdentifier>>>(), Arg.Any<CancellationToken>());
    }

    private static ReservePlace NewCommand()
    {
        var employee = EmployeeIdentifier.New();
        return new(company, employee, employee, OfficeIdentifier.New(), RoomIdentifier.New(), bookingDate, ActorIsAdmin: false);
    }

    private static IAttendanceDayRepository RepositoryThatApplies()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.MutateAsync(
            Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(),
            Arg.Any<Func<AttendanceDay, Result<ReservationIdentifier>>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(
                call.Arg<Func<AttendanceDay, Result<ReservationIdentifier>>>()(AttendanceDay.For(company, bookingDate))));
        return repository;
    }

    private static IBusinessClock ClockAt(DateTimeOffset instant, BookingDate today)
    {
        var clock = Substitute.For<IBusinessClock>();
        clock.Now.Returns(instant);
        clock.Today.Returns(today);
        return clock;
    }

    private static IRoomDirectory RoomDirectoryWith(int? capacity)
    {
        var rooms = Substitute.For<IRoomDirectory>();
        rooms.FindCapacityAsync(Arg.Any<RoomIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(capacity is null
                ? Result.Failure<RoomCapacity>(Error.NotFound("unknown_room", "The room is not known."))
                : Result.Success(RoomCapacity.From(capacity.Value)));
        return rooms;
    }
}
