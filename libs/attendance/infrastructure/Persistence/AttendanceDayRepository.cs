using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

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
            context.ChangeTracker.Clear();
            return Error.Conflict(
                "concurrency_conflict",
                "The attendance day was modified concurrently.");
        }

        attendanceDay.ClearUncommittedEvents();
        return Result.Success();
    }

    public Task<Result<TResult>> MutateAsync<TResult>(
        CompanyIdentifier company,
        BookingDate date,
        Func<AttendanceDay, Result<TResult>> decide,
        CancellationToken cancellationToken) =>
        OptimisticWrite.ExecuteAsync(
            () => LoadAsync(company, date, cancellationToken),
            decide,
            attendanceDay => SaveAsync(attendanceDay, cancellationToken));

    public Task<Result> MutateAsync(
        CompanyIdentifier company,
        BookingDate date,
        Func<AttendanceDay, Result> decide,
        CancellationToken cancellationToken) =>
        OptimisticWrite.ExecuteAsync(
            () => LoadAsync(company, date, cancellationToken),
            decide,
            attendanceDay => SaveAsync(attendanceDay, cancellationToken));
}
