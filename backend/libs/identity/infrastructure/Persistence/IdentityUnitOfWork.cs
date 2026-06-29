using SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

public sealed class IdentityUnitOfWork(IdentityDbContext context, IIntegrationEventOutbox outbox, TimeProvider timeProvider)
    : OutboxUnitOfWork(context, outbox, timeProvider, IdentityIntegrationEventMap.ToIntegrationEvent);
