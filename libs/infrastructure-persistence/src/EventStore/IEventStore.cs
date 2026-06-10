namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// The owned abstraction over the hand-rolled, append-only event store (ADR-0012). It exposes only
/// what event-sourced repositories need — append a batch with an optimistic-concurrency check, and
/// replay a stream — keeping the EF Core / Npgsql implementation an infrastructure detail.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends <paramref name="events"/> to <paramref name="streamId"/> as the next contiguous
    /// versions, asserting the stream is currently at <paramref name="expectedVersion"/>. Use
    /// <see cref="StreamVersion.None"/> to assert the stream does not exist yet.
    /// </summary>
    /// <exception cref="EventStoreConcurrencyException">
    /// The stream's current version differs from <paramref name="expectedVersion"/>.
    /// </exception>
    Task AppendAsync(
        StreamId streamId,
        StreamVersion expectedVersion,
        IReadOnlyList<object> events,
        EventMetadata metadata,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a stream's events in version order, ready to be replayed to rebuild an aggregate.
    /// Returns an empty list for a stream that has no events.
    /// </summary>
    Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(StreamId streamId, CancellationToken cancellationToken);
}
