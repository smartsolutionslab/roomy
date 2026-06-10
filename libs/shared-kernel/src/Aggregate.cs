namespace SmartSolutionsLab.Roomy.SharedKernel;

public abstract class Aggregate : IAggregate
{
    private readonly List<IDomainEvent> domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents;

    public void ClearDomainEvents() => domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);
}
