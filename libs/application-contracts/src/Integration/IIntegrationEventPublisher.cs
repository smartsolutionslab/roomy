namespace SmartSolutionsLab.Roomy.Application.Contracts.Integration;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
