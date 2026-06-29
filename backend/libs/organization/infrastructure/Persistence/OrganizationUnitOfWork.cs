using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class OrganizationUnitOfWork(OrganizationDbContext context, IIntegrationEventOutbox outbox, TimeProvider timeProvider)
    : OutboxUnitOfWork(context, outbox, timeProvider, OrganizationIntegrationEventMap.ToIntegrationEvent);
