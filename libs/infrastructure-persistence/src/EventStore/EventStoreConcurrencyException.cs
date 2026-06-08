namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// Thrown when an append's asserted expected version does not match the stream's current version —
/// another writer advanced the stream first (optimistic concurrency, ADR-0012). The unique
/// <c>(stream_id, version)</c> constraint guarantees this is detected at the database, not only in
/// memory; the infrastructure surfaces that as this exception so callers can retry or fail.
/// </summary>
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
