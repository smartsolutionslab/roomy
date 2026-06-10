namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public sealed record EventEnvelope(
    object Event,
    StreamId StreamId,
    StreamVersion Version,
    long GlobalSequence,
    EventMetadata Metadata,
    DateTimeOffset OccurredOnUtc);
