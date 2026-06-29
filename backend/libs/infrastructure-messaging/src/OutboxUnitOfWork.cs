using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public abstract class OutboxUnitOfWork(
    DbContext context,
    IIntegrationEventOutbox outbox,
    TimeProvider timeProvider,
    Func<IDomainEvent, DateTimeOffset, IIntegrationEvent?> toIntegrationEvent) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var aggregates = context.ChangeTracker.Entries<Aggregate>()
            .Select(entry => entry.Entity)
            .ToList();

        var occurredAt = timeProvider.GetUtcNow();
        var integrationEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .Select(domainEvent => toIntegrationEvent(domainEvent, occurredAt))
            .OfType<IIntegrationEvent>()
            .ToList();

        await outbox.SaveAndPublishAsync(context, integrationEvents, cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
