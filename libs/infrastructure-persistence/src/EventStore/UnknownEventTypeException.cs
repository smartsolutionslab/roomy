namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public sealed class UnknownEventTypeException : Exception
{
    public UnknownEventTypeException(string message)
        : base(message)
    {
    }

    public UnknownEventTypeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UnknownEventTypeException()
    {
    }
}
