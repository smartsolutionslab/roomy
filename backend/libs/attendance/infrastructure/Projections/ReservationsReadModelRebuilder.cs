using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;

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
