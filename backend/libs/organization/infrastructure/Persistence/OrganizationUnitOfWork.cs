using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class OrganizationUnitOfWork(OrganizationDbContext context, IIntegrationEventOutbox outbox, TimeProvider timeProvider)
    : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var aggregates = context.ChangeTracker.Entries<Aggregate>()
            .Select(entry => entry.Entity)
            .ToList();

        var occurredAt = timeProvider.GetUtcNow();
        var integrationEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .Select(domainEvent => OrganizationIntegrationEventMap.ToIntegrationEvent(domainEvent, occurredAt))
            .OfType<IIntegrationEvent>()
            .ToList();

        await outbox.SaveAndPublishAsync(context, integrationEvents, cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
