namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public sealed class StoredEvent
{
    public long GlobalSequence { get; init; }

    public Guid StreamId { get; init; }

    public int Version { get; init; }

    public required string EventType { get; init; }

    public required string Payload { get; init; }

    public required string Metadata { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; }
}
