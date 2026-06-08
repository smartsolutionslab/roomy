namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// A deserialized event read back from a stream: the domain <see cref="Event"/> itself plus the
/// positional and ambient context the store recorded around it. This is what a repository replays
/// to rebuild an aggregate (ADR-0012: events are the source of truth).
/// </summary>
/// <param name="Event">The reconstructed domain event instance.</param>
/// <param name="StreamId">The stream the event belongs to.</param>
/// <param name="Version">The event's 1-based position within its stream.</param>
/// <param name="GlobalSequence">The event's global, monotonic order across all streams.</param>
/// <param name="Metadata">The ambient context captured at append time.</param>
/// <param name="OccurredOnUtc">When the event was appended, in UTC.</param>
public sealed record EventEnvelope(
    object Event,
    StreamId StreamId,
    StreamVersion Version,
    long GlobalSequence,
    EventMetadata Metadata,
    DateTimeOffset OccurredOnUtc);
