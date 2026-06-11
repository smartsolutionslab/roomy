using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using Wolverine.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public sealed class WolverineIntegrationEventOutbox(IDbContextOutbox outbox) : IIntegrationEventOutbox
{
    public async Task SaveAndPublishAsync(DbContext context, IReadOnlyCollection<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken)
    {
        outbox.Enroll(context);

        foreach (var integrationEvent in integrationEvents)
        {
            await outbox.PublishAsync(integrationEvent).ConfigureAwait(false);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }
}
