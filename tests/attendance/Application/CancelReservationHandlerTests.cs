using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
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
        var repository = new FakeRepository(reservationId, owner);
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, reservationId, bookingDate, owner, ActorIsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        repository.LoadCount.ShouldBe(1);
        repository.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_unknown_reservation_is_rejected_without_saving()
    {
        var repository = new FakeRepository(seededReservation: null, seededOwner: default);
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, ReservationIdentifier.New(), bookingDate, EmployeeIdentifier.New(), ActorIsAdmin: false),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("reservation_not_found");
        repository.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_concurrency_conflict_is_retried_and_can_then_succeed()
    {
        var reservationId = ReservationIdentifier.New();
        var owner = EmployeeIdentifier.New();
        var repository = new FakeRepository(reservationId, owner);
        repository.EnqueueSaveResult(Error.Conflict("concurrency_conflict", "The day changed concurrently."));
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, reservationId, bookingDate, owner, ActorIsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        repository.LoadCount.ShouldBe(2);
        repository.SaveCount.ShouldBe(2);
    }

    [Fact]
    public async Task Exhausting_the_retries_returns_a_retryable_conflict()
    {
        var reservationId = ReservationIdentifier.New();
        var owner = EmployeeIdentifier.New();
        var repository = new FakeRepository(reservationId, owner);
        repository.AlwaysConflictOnSave();
        var handler = new CancelReservationHandler(repository, new FixedTimeProvider(now));

        var result = await handler.HandleAsync(
            new CancelReservation(company, reservationId, bookingDate, owner, ActorIsAdmin: false),
            CancellationToken.None);

        result.Error.Code.ShouldBe("concurrency_retry_exhausted");
        repository.SaveCount.ShouldBe(3);
    }

    private sealed class FakeRepository(ReservationIdentifier? seededReservation, EmployeeIdentifier seededOwner)
        : IAttendanceDayRepository
    {
        private readonly Queue<Result> saveResults = new();
        private bool alwaysConflict;

        public int LoadCount { get; private set; }

        public int SaveCount { get; private set; }

        public void EnqueueSaveResult(Result result) => saveResults.Enqueue(result);

        public void AlwaysConflictOnSave() => alwaysConflict = true;

        public Task<AttendanceDay> LoadAsync(CompanyIdentifier company, BookingDate date, CancellationToken cancellationToken)
        {
            LoadCount++;
            var day = AttendanceDay.For(company, date);
            if (seededReservation is { } reservation)
            {
                day.LoadFromHistory(
                [
                    new ReservationPlaced(reservation.Value, company.Value, date.Value, seededOwner.Value, Guid.CreateVersion7(), Guid.CreateVersion7(), now),
                ]);
            }

            return Task.FromResult(day);
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
