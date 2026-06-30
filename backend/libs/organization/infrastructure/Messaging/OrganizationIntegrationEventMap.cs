using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees.Events;
using SmartSolutionsLab.Roomy.SharedKernel;
using DomainEvents = SmartSolutionsLab.Roomy.Organization.Domain.Offices.Events;
using IntegrationContracts = SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;

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

            EmployeeHired hired => new IntegrationContracts.EmployeeHired(
                hired.Employee.Value,
                hired.User.Value,
                hired.Email.Value,
                hired.Name.Value,
                ToHiredRole(hired.Role),
                hired.InitialCredential.Value,
                occurredAt),

            _ => null,
        };

    private static IntegrationContracts.HiredRole ToHiredRole(EmployeeRole role) =>
        role == EmployeeRole.Administrator
            ? IntegrationContracts.HiredRole.Administrator
            : IntegrationContracts.HiredRole.Employee;
}
