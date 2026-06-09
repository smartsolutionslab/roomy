using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// The event-sourced repository for AttendanceDay (ADR-0039) over the hand-rolled event store
// (ADR-0012). Load replays the stream onto a fresh aggregate for the company-day; Save stages the
// occupancy projection (ADR-0038) and appends the uncommitted events at the aggregate's expected
// version, so the read-model rows and the events commit in one transaction (FR-010). The store's
// optimistic-concurrency exception is mapped to Error.Conflict so it never escapes the infrastructure
// (research R2); the change tracker is reset first so a losing attempt's staged rows do not leak into
// the application's bounded retry.
public sealed class AttendanceDayRepository(
    IEventStore eventStore,
    IReservationProjection projection,
    AttendanceDbContext context) : IAttendanceDayRepository
{
    public async Task<AttendanceDay> LoadAsync(
        CompanyIdentifier company,
        BookingDate date,
        CancellationToken cancellationToken)
    {
        var streamId = AttendanceDayStreamId.For(company, date);
        var stream = await eventStore.ReadStreamAsync(streamId, cancellationToken).ConfigureAwait(false);

        var attendanceDay = AttendanceDay.For(company, date);
        attendanceDay.LoadFromHistory(stream.Select(envelope => envelope.Event));

        return attendanceDay;
    }

    public async Task<Result> SaveAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken)
    {
        if (attendanceDay.UncommittedEvents.Count == 0)
        {
            return Result.Success();
        }

        var streamId = AttendanceDayStreamId.For(attendanceDay.Company, attendanceDay.Date);
        var expectedVersion = StreamVersion.From(attendanceDay.Version);

        // Stage the occupancy read-model rows on the shared context, then append the events: the append's
        // SaveChanges commits both in one transaction (ADR-0038), so a view reflects the booking the
        // moment it commits (FR-010).
        await projection.ApplyAsync(attendanceDay.UncommittedEvents, cancellationToken).ConfigureAwait(false);

        try
        {
            await eventStore.AppendAsync(
                streamId,
                expectedVersion,
                attendanceDay.UncommittedEvents,
                EventMetadata.None,
                cancellationToken).ConfigureAwait(false);
        }
        catch (EventStoreConcurrencyException)
        {
            // Discard this attempt's staged events and read-model rows so the application's bounded retry
            // re-projects against freshly reloaded state (research R4) — the scoped context is reused.
            context.ChangeTracker.Clear();
            return Error.Conflict(
                "concurrency_conflict",
                "The attendance day was modified concurrently.");
        }

        attendanceDay.ClearUncommittedEvents();
        return Result.Success();
    }
}
