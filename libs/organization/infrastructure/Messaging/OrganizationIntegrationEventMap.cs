using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.SharedKernel;
using DomainEvents = SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using IntegrationContracts = SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;

// Maps organization's domain events to its published-language contracts at the infrastructure edge
// (ADR-0031/0037) — the outbound mirror of how a consumer maps a wire event to an internal command.
// The unit of work drains domain events and calls this to build the integration events it stages in the
// outbox; OccurredAt is stamped here from the caller's clock. A domain event with no published
// counterpart maps to null and is skipped.
internal static class OrganizationIntegrationEventMap
{
    public static IIntegrationEvent? ToIntegrationEvent(IDomainEvent domainEvent, DateTimeOffset occurredAt) =>
        domainEvent switch
        {
            DomainEvents.OfficeOpened opened => new IntegrationContracts.OfficeOpened(
                opened.Office.Value,
                opened.Company.Value,
                opened.Name.Value,
                opened.Location.Value,
                occurredAt),

            DomainEvents.RoomAdded added => new IntegrationContracts.RoomAdded(
                added.Room.Value,
                added.Office.Value,
                added.Company.Value,
                added.Name.Value,
                added.Capacity.Value,
                occurredAt),

            _ => null,
        };
}
