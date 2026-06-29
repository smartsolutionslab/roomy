using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Identity.Domain.Users.Events;
using SmartSolutionsLab.Roomy.SharedKernel;
using IntegrationContracts = SmartSolutionsLab.Roomy.Contracts.Identity;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;

internal static class IdentityIntegrationEventMap
{
    public static IIntegrationEvent? ToIntegrationEvent(IDomainEvent domainEvent, DateTimeOffset occurredAt) =>
        domainEvent switch
        {
            AdministratorGranted granted => new IntegrationContracts.AdministratorGranted(granted.UserId.Value, occurredAt),

            _ => null,
        };
}
