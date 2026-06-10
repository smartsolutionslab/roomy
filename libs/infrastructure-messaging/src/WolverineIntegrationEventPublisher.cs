using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using Wolverine;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public sealed class WolverineIntegrationEventPublisher(IMessageBus messageBus) : IIntegrationEventPublisher
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await messageBus.PublishAsync(integrationEvent).ConfigureAwait(false);
    }
}
