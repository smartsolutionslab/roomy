namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// Thrown when the event-type registry is asked to map a CLR type or persisted name it does not
/// know — a configuration fault (a missing registration), not an expected business outcome.
/// </summary>
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
