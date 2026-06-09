namespace SmartSolutionsLab.Roomy.SharedKernel;

// Base for an aggregate root — an entity that is also a consistency boundary (ADR-0003) and the source
// of its domain events (ADR-0032). An aggregate records events as it mutates; the unit of work drains
// them on commit. Recording carries no dispatch dependency, so the domain stays framework-free
// (ADR-0005). IAggregate remains the marker the architecture tests key on.
public abstract class Aggregate : IAggregate
{
    private readonly List<IDomainEvent> domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents;

    public void ClearDomainEvents() => domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);
}
