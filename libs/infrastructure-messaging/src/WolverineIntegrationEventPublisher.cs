using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;
using Wolverine;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

/// <summary>
/// The Wolverine-backed implementation of the owned <see cref="IIntegrationEventPublisher"/> port
/// (ADR-0005). It adapts the application's framework-free publish call onto Wolverine's
/// <see cref="IMessageBus"/>, which — when resolved from a scope that has been enrolled in the
/// EF Core transaction (see <c>MessagingServiceCollectionExtensions</c>) — captures the message in
/// Wolverine's durable transactional outbox so the publish commits atomically with the aggregate
/// write (ADR-0012:76). Routing to the configured transport (RabbitMQ by default, ADR-0015) is by
/// message type; this adapter stays transport-agnostic.
/// </summary>
public sealed class WolverineIntegrationEventPublisher(IMessageBus messageBus) : IIntegrationEventPublisher
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await messageBus.PublishAsync(integrationEvent).ConfigureAwait(false);
    }
}
