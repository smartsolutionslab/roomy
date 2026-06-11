using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;
using Wolverine;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public sealed class WolverineIntegrationEventPublisher(IMessageBus messageBus) : IIntegrationEventPublisher
{
    private readonly IMessageBus messageBus = Ensure.That((IMessageBus?)messageBus).IsNotNull().Value;

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        Ensure.That((IIntegrationEvent?)integrationEvent).IsNotNull();

        cancellationToken.ThrowIfCancellationRequested();

        await messageBus.PublishAsync(integrationEvent).ConfigureAwait(false);
    }
}
