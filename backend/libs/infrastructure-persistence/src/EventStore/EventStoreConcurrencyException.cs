namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public sealed class EventStoreConcurrencyException : Exception
{
    public EventStoreConcurrencyException(StreamId streamId, StreamVersion expectedVersion, StreamVersion actualVersion)
        : base(
            $"Concurrency conflict appending to stream '{streamId}': expected version "
            + $"{expectedVersion} but the stream is at version {actualVersion}.")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public EventStoreConcurrencyException(string message)
        : base(message)
    {
    }

    public EventStoreConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public EventStoreConcurrencyException()
    {
    }

    public StreamId StreamId { get; }

    public StreamVersion ExpectedVersion { get; }

    public StreamVersion ActualVersion { get; }
}
