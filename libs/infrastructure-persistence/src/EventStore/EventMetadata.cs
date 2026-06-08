namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// Ambient context captured with each appended event, independent of the event's own payload:
/// correlation/causation ids for tracing a flow across handlers, and the acting subject. Stored as
/// <c>jsonb</c> metadata on the row (ADR-0012).
/// </summary>
public sealed record EventMetadata(
    Guid? CorrelationId = null,
    Guid? CausationId = null,
    string? ActorId = null)
{
    /// <summary>Empty metadata, for appends with no ambient context.</summary>
    public static EventMetadata None { get; } = new();
}
