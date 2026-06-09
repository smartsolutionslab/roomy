namespace SmartSolutionsLab.Roomy.SharedKernel;

// Base for an event-sourced aggregate root (ADR-0039). Unlike the state-based Aggregate (ADR-0032),
// whose state lives in mapped fields and whose IDomainEvents are an optional side-record, here state
// IS the left-fold over the aggregate's event stream (ADR-0012): every change happens in Apply, and
// nowhere else. Raise applies an event and collects it for the repository to append; LoadFromHistory
// replays the persisted stream to reconstruct the instance. It stays framework-free — the event store
// is touched only at the infrastructure edge — and carries IAggregate, the marker the architecture
// tests key on.
//
// Version is the count of *persisted* events the stream is at (0 for a never-written stream). It is the
// expected version the repository asserts when appending uncommitted events (optimistic concurrency).
// Uncommitted events do not advance it; the infrastructure maps this count to the event store's
// StreamVersion at the edge, keeping the shared kernel free of any persistence type.
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
