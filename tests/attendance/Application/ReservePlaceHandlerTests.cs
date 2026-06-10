using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The reserve use case: read capacity from the room directory, then load → decide → save inside a
// bounded optimistic-retry loop (research R2). On a concurrency conflict it reloads and re-decides,
// so the loser of the last-place race (scenario 12) is rejected as room_full rather than overwriting.
// Driven here against substituted ports and a fixed clock — no infrastructure.
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
        var handler = new ReservePlaceHandler(repository, RoomDirectoryWith(capacity: null), new FixedTimeProvider(now));

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
            .Returns(_ => FullRoomDay(command)); // the room is already full on load
        var handler = NewHandler(repository, capacity: 1);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.Error.Code.ShouldBe("room_full");
        await repository.Received(1).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>()); // no save attempted on a domain failure
    }

    [Fact]
    public async Task A_concurrency_conflict_is_retried_and_can_then_succeed()
    {
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate));
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(ConcurrencyConflict(), Result.Success()); // first save loses the race, then succeeds
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await repository.Received(2).LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>()); // reloaded after the conflict
        await repository.Received(2).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_loser_of_the_last_place_race_is_rejected_as_room_full_on_reload()
    {
        // Scenario 12: the first attempt decides "available" but loses the save race; on reload the
        // winner has taken the last place, so the re-decision is room_full — capacity never exceeded.
        var command = NewCommand();
        var repository = Substitute.For<IAttendanceDayRepository>();
        repository.LoadAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<BookingDate>(), Arg.Any<CancellationToken>())
            .Returns(_ => AttendanceDay.For(company, bookingDate), _ => FullRoomDay(command)); // empty, then full on reload
        repository.SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>())
            .Returns(ConcurrencyConflict()); // first save conflicts
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
        await repository.Received(3).SaveAsync(Arg.Any<AttendanceDay>(), Arg.Any<CancellationToken>()); // the bounded number of attempts
    }

    private static ReservePlace NewCommand() =>
        new(company, EmployeeIdentifier.New(), OfficeIdentifier.New(), RoomIdentifier.New(), bookingDate);

    private static ReservePlaceHandler NewHandler(IAttendanceDayRepository repository, int capacity) =>
        new(repository, RoomDirectoryWith(capacity), new FixedTimeProvider(now));

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
