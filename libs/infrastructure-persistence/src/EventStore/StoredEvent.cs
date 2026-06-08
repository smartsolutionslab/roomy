namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// One row of the append-only events table (ADR-0012): an event belonging to a stream, at a
/// per-stream <see cref="Version"/>, ordered globally by the monotonic <see cref="GlobalSequence"/>.
/// The unique constraint on <c>(stream_id, version)</c> is what enforces optimistic concurrency at
/// the database. This is the persistence record; readers receive the richer <see cref="EventEnvelope"/>.
/// </summary>
public sealed class StoredEvent
{
    /// <summary>Monotonic, globally-ordered sequence assigned by the database (<c>bigserial</c>).</summary>
    public long GlobalSequence { get; init; }

    /// <summary>The owning stream (aggregate) id.</summary>
    public Guid StreamId { get; init; }

    /// <summary>The 1-based position of this event within its stream.</summary>
    public int Version { get; init; }

    /// <summary>The stable persisted event type name (see <see cref="IEventTypeRegistry"/>).</summary>
    public required string EventType { get; init; }

    /// <summary>The serialized event body (stored as <c>jsonb</c>).</summary>
    public required string Payload { get; init; }

    /// <summary>Serialized envelope metadata (correlation, causation, actor) as <c>jsonb</c>.</summary>
    public required string Metadata { get; init; }

    /// <summary>When the event was appended, in UTC.</summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}
