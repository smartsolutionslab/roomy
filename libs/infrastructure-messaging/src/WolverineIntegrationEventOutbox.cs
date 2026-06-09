using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using Wolverine.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

/// <summary>
/// The Wolverine-backed <see cref="IIntegrationEventOutbox"/> (ADR-0037). It enrolls the caller's
/// <see cref="DbContext"/> in Wolverine's durable EF Core outbox, stages each integration event, then
/// <see cref="IDbContextOutbox.SaveChangesAndFlushMessagesAsync"/> — which persists the context and the
/// outbox rows in the one transaction and relays the messages afterwards. This is the only place the
/// transactional publish-from-a-use-case path touches Wolverine (ADR-0005).
/// </summary>
public sealed class WolverineIntegrationEventOutbox(IDbContextOutbox outbox) : IIntegrationEventOutbox
{
    public async Task SaveAndPublishAsync(
        DbContext context,
        IReadOnlyCollection<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken)
    {
        outbox.Enroll(context);

        foreach (var integrationEvent in integrationEvents)
        {
            await outbox.PublishAsync(integrationEvent).ConfigureAwait(false);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }
}
