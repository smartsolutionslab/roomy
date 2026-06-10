using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;

// Re-derives the Reservations read model from the source of truth — the company-day event streams
// (ADR-0026/0038, research R5). The inline projection only moves the read model forward; a projector fix,
// a schema change, or recovery needs this offline rebuild. It truncates the read model and replays every
// stream through the same ReservationProjection the write path uses, in one transaction so a reader never
// observes a partially rebuilt model. The streams stay authoritative, so the rebuild is a pure function of
// the log. (Offices/Employees are re-derived by replaying their integration-event feeds — out of scope.)
public sealed class ReservationsReadModelRebuilder(
    IEventStore eventStore,
    IReservationProjection projection,
    AttendanceDbContext context)
{
    public async Task RebuildAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await context.Reservations.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        var streamIds = await context.Events
            .Select(storedEvent => storedEvent.StreamId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var streamId in streamIds)
        {
            var stream = await eventStore
                .ReadStreamAsync(StreamId.From(streamId), cancellationToken).ConfigureAwait(false);
            await projection
                .ApplyAsync([.. stream.Select(envelope => envelope.Event)], cancellationToken)
                .ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
