namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public interface IEventStore
{
    Task AppendAsync(
        StreamId streamId,
        StreamVersion expectedVersion,
        IReadOnlyList<object> events,
        EventMetadata metadata,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EventEnvelope>> ReadStreamAsync(StreamId streamId, CancellationToken cancellationToken);
}
