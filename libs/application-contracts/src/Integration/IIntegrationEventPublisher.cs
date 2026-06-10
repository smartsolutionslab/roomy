namespace SmartSolutionsLab.Roomy.Application.Contracts.Integration;

/// <summary>
/// The outbound port the application uses to publish an <see cref="IIntegrationEvent"/>. This is
/// the seam that keeps the messaging framework at the edge: the application defines this port and
/// the Wolverine-backed transactional outbox implements it in infrastructure (ADR-0005).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
