using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The reserve use case: read capacity from the room directory, then load → decide → save inside a
// bounded optimistic-retry loop (research R2). On a concurrency conflict it reloads and re-decides,
// so the loser of the last-place race (scenario 12) is rejected as room_full rather than overwriting.
// Driven here against an in-memory repository and a fixed clock — no infrastructure.
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
        var repository = new FakeRepository();
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldNotBe(Guid.Empty);
        repository.SaveCount.ShouldBe(1);
        repository.LoadCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_unknown_room_is_rejected_without_touching_the_aggregate()
    {
        var repository = new FakeRepository();
        var handler = new ReservePlaceHandler(repository, new StubRoomDirectory(capacity: null), new FixedTimeProvider(now));

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_room");
        repository.LoadCount.ShouldBe(0);
        repository.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_domain_rejection_is_returned_without_retrying()
    {
        var command = NewCommand();
        var repository = new FakeRepository();
        repository.EnqueueDay(FullRoomDay(command)); // the room is already full on load
        var handler = NewHandler(repository, capacity: 1);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.Error.Code.ShouldBe("room_full");
        repository.LoadCount.ShouldBe(1);
        repository.SaveCount.ShouldBe(0); // no save attempted on a domain failure
    }

    [Fact]
    public async Task A_concurrency_conflict_is_retried_and_can_then_succeed()
    {
        var repository = new FakeRepository();
        repository.EnqueueSaveResult(ConcurrencyConflict()); // first save loses the race
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        repository.LoadCount.ShouldBe(2); // reloaded after the conflict
        repository.SaveCount.ShouldBe(2);
    }

    [Fact]
    public async Task The_loser_of_the_last_place_race_is_rejected_as_room_full_on_reload()
    {
        // Scenario 12: the first attempt decides "available" but loses the save race; on reload the
        // winner has taken the last place, so the re-decision is room_full — capacity never exceeded.
        var command = NewCommand();
        var repository = new FakeRepository();
        repository.EnqueueDay(AttendanceDay.For(company, bookingDate)); // first load: empty
        repository.EnqueueDay(FullRoomDay(command));                    // reload: now full
        repository.EnqueueSaveResult(ConcurrencyConflict());            // first save conflicts
        var handler = NewHandler(repository, capacity: 1);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.Error.Code.ShouldBe("room_full");
        repository.LoadCount.ShouldBe(2);
        repository.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Exhausting_the_retries_returns_a_retryable_conflict()
    {
        var repository = new FakeRepository();
        repository.AlwaysConflictOnSave();
        var handler = NewHandler(repository, capacity: 8);

        var result = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        result.Error.Code.ShouldBe("concurrency_retry_exhausted");
        repository.SaveCount.ShouldBe(3); // the bounded number of attempts
    }

    private static ReservePlace NewCommand() =>
        new(company, EmployeeIdentifier.New(), OfficeIdentifier.New(), RoomIdentifier.New(), bookingDate);

    private static ReservePlaceHandler NewHandler(FakeRepository repository, int capacity) =>
        new(repository, new StubRoomDirectory(capacity), new FixedTimeProvider(now));

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

    private sealed class StubRoomDirectory(int? capacity) : IRoomDirectory
    {
        public Task<Result<RoomCapacity>> FindCapacityAsync(RoomIdentifier room, CancellationToken cancellationToken) =>
            Task.FromResult(capacity is null
                ? Result.Failure<RoomCapacity>(Error.NotFound("unknown_room", "The room is not known."))
                : Result.Success(RoomCapacity.From(capacity.Value)));
    }

    private sealed class FakeRepository : IAttendanceDayRepository
    {
        private readonly Queue<AttendanceDay> days = new();
        private readonly Queue<Result> saveResults = new();
        private bool alwaysConflict;

        public int LoadCount { get; private set; }

        public int SaveCount { get; private set; }

        public void EnqueueDay(AttendanceDay day) => days.Enqueue(day);

        public void EnqueueSaveResult(Result result) => saveResults.Enqueue(result);

        public void AlwaysConflictOnSave() => alwaysConflict = true;

        public Task<AttendanceDay> LoadAsync(CompanyIdentifier company, BookingDate date, CancellationToken cancellationToken)
        {
            LoadCount++;
            return Task.FromResult(days.Count > 0 ? days.Dequeue() : AttendanceDay.For(company, date));
        }

        public Task<Result> SaveAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken)
        {
            SaveCount++;
            if (alwaysConflict)
            {
                return Task.FromResult<Result>(Error.Conflict("concurrency_conflict", "The day changed concurrently."));
            }

            return Task.FromResult(saveResults.Count > 0 ? saveResults.Dequeue() : Result.Success());
        }
    }
}
