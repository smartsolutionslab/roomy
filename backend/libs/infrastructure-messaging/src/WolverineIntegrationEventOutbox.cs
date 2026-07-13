using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public sealed class WolverineIntegrationEventOutbox(IDbContextOutbox outbox, IMessageBus messageBus) : IIntegrationEventOutbox
{
    public async Task SaveAndPublishAsync(DbContext context, IReadOnlyCollection<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken)
    {
        // Inside a Wolverine message handler, AutoApplyTransactions has already begun the DbContext
        // transaction and will save, commit, and flush the outbox once the handler returns. Publishing
        // on the ambient bus enrols the events in that same transaction; committing or flushing here
        // (as SaveChangesAndFlushMessagesAsync does) would close the connection out from under the
        // middleware's own commit.
        if (context.Database.CurrentTransaction is not null)
        {
            foreach (var integrationEvent in integrationEvents)
            {
                await messageBus.PublishAsync(integrationEvent).ConfigureAwait(false);
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // No ambient transaction (an HTTP request): own the outbox — enrol the DbContext, stage the
        // events, and save-commit-flush as one unit.
        outbox.Enroll(context);

        foreach (var integrationEvent in integrationEvents)
        {
            await outbox.PublishAsync(integrationEvent).ConfigureAwait(false);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }
}
