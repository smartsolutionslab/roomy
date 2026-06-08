namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// Thrown when a stored event payload cannot be reconstructed into an event instance — a corrupt
/// or incompatible log row, treated as an infrastructure fault rather than a business outcome.
/// </summary>
public sealed class EventDeserializationException : Exception
{
    public EventDeserializationException(string message)
        : base(message)
    {
    }

    public EventDeserializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public EventDeserializationException()
    {
    }
}
