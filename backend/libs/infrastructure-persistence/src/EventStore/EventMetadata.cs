namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public sealed record EventMetadata(
    Guid? CorrelationId = null,
    Guid? CausationId = null,
    string? ActorId = null)
{
    public static EventMetadata None { get; } = new();
}
