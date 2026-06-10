using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

public interface IIntegrationEventOutbox
{
    Task SaveAndPublishAsync(DbContext context, IReadOnlyCollection<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken);
}
