using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// The event-sourced repository for AttendanceDay (ADR-0036) over the hand-rolled event store
// (ADR-0012). Load replays the stream onto a fresh aggregate for the company-day; Save appends the
// uncommitted events at the aggregate's expected version. The store's optimistic-concurrency exception
// is mapped to Error.Conflict so it never escapes the infrastructure (research R2), letting the
// application reload and re-decide.
public sealed class AttendanceDayRepository(IEventStore eventStore) : IAttendanceDayRepository
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
            return Error.Conflict(
                "concurrency_conflict",
                "The attendance day was modified concurrently.");
        }

        attendanceDay.ClearUncommittedEvents();
        return Result.Success();
    }
}
