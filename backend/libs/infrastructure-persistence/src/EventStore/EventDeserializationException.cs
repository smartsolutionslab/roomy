namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

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
