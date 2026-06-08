namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Outbox;

/// <summary>
/// A row in the transactional outbox: an integration message captured in the <em>same</em>
/// database transaction as the state change that produced it, so the write and the intent to
/// publish commit atomically (ADR-0012). A relay later reads unprocessed rows and publishes them,
/// marking <see cref="ProcessedOnUtc"/>. This type is the persisted record only — the Wolverine
/// relay that drains it is out of scope here and lands in #20 (ADR-0005).
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Stable identity of the message; also the idempotency key for the relay.</summary>
    public Guid Id { get; init; }

    /// <summary>The resolvable event type name used to deserialize <see cref="Payload"/>.</summary>
    public required string Type { get; init; }

    /// <summary>The serialized integration event body (stored as <c>jsonb</c>).</summary>
    public required string Payload { get; init; }

    /// <summary>When the message was enqueued, in UTC.</summary>
    public DateTimeOffset OccurredOnUtc { get; init; }

    /// <summary>When the relay successfully published the message, or <c>null</c> while pending.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    /// <summary>The last publish error, if a delivery attempt failed; <c>null</c> otherwise.</summary>
    public string? Error { get; set; }
}
