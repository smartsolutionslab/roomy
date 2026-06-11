namespace SmartSolutionsLab.Roomy.SharedKernel;

public abstract class EventSourcedAggregate : IAggregate
{
    private readonly List<object> uncommittedEvents = [];

    public int Version { get; private set; }

    public IReadOnlyList<object> UncommittedEvents => uncommittedEvents;

    public void LoadFromHistory(IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
        }
    }

    public void ClearUncommittedEvents() => uncommittedEvents.Clear();

    protected void Raise(object @event)
    {
        Apply(@event);
        uncommittedEvents.Add(@event);
    }

    protected abstract void Apply(object @event);
}
