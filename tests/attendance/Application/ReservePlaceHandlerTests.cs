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
    public async Task Reserving_an_available_room_succeeds_and_saves_once()
    {
        var command = NewCommand();
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldNotBe(Guid.Empty);
        await repository.Received(1).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_room_is_rejected_without_touching_the_aggregate()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        var handler = new ReservePlaceHandler(repository, RoomDirectoryWith(capacity: null), ClockAt(now, bookingDate));

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_room");
        await repository.DidNotReceive().LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_domain_rejection_is_returned_without_retrying()
    {
        var command = NewCommand();
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => FullRoomDay(command));
        var handler = NewHandler(repository, capacity: 1);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.Error.Code.ShouldBe("room_full");
        await repository.Received(1).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_concurrency_conflict_is_retried_and_can_then_succeed()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(ConcurrencyConflict(), Result.Success());
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await repository.Received(2).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.Received(2).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_loser_of_the_last_place_race_is_rejected_as_room_full_on_reload()
    {
        var command = NewCommand();
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate), _ => FullRoomDay(command));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(ConcurrencyConflict());
        var handler = NewHandler(repository, capacity: 1);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.Error.Code.ShouldBe("room_full");
        await repository.Received(2).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Exhausting_the_retries_returns_a_retryable_conflict()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(ConcurrencyConflict());
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.Error.Code.ShouldBe("concurrency_retry_exhausted");
        await repository.Received(3).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_non_administrator_reserving_for_another_employee_is_forbidden_without_touching_the_aggregate()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        var onBehalf = NewCommand() with { Employee = EmployeeIdentifier.New(), ActorIsAdmin = false };
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(onBehalf, CancellationToken.None);

        result.Error.Code.ShouldBe("not_authorized");
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        await repository.DidNotReceive().LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_administrator_may_reserve_for_another_employee()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var onBehalf = NewCommand() with { Employee = EmployeeIdentifier.New(), ActorIsAdmin = true };
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(onBehalf, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await repository.Received(1).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    private static ReservePlace NewCommand()
    {
        var employee = EmployeeIdentifier.New();
        return new(company, employee, employee, OfficeIdentifier.New(), RoomIdentifier.New(), bookingDate, ActorIsAdmin: false);
    }

    private static ReservePlaceHandler NewHandler(IAttendanceDayRepository repository, int capacity) =>
        new(repository, RoomDirectoryWith(capacity), ClockAt(now, bookingDate));

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

    private static AttendanceDay FullRoomDay(ReservePlace command)
    {
        var day = AttendanceDay.For(company, bookingDate);
        day.LoadFromHistory(
        [
            new ReservationPlaced(Guid.CreateVersion7(), company.Value, bookingDate.Value, Guid.CreateVersion7(), command.Office.Value, command.Room.Value, now),
        ]);
        return day;
    }

    private static Result ConcurrencyConflict() =>
        Error.Conflict("concurrency_conflict", "The day changed concurrently.");
}
