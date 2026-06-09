using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Organization.Application;
using SmartSolutionsLab.Roomy.Organization.Application.UseCases;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure;

// Composition-root wiring for the organization context's infrastructure. Keeps EF Core registration
// out of the host's Program.cs (ADR-0003/0012). No messaging or external-provider adapters this slice
// (research.md D4) — office/room management publishes nothing and provisions nothing.
public static class OrganizationInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddRoomyDbContext<OrganizationDbContext>(connectionString);
        services.AddScoped<IOfficeRepository, OfficeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IUnitOfWork, OrganizationUnitOfWork>();

        // The unit of work stamps OccurredAt on the integration events it drains to the outbox (ADR-0037).
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    public static IServiceCollection AddOrganizationUseCases(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateOffice, OfficeIdentifier>, CreateOfficeHandler>();
        services.AddScoped<ICommandHandler<AddRoomToOffice, RoomIdentifier>, AddRoomToOfficeHandler>();
        services.AddScoped<ICommandHandler<RenameOffice>, RenameOfficeHandler>();
        services.AddScoped<ICommandHandler<ChangeOfficeLocation>, ChangeOfficeLocationHandler>();
        services.AddScoped<ICommandHandler<RenameRoom>, RenameRoomHandler>();

        return services;
    }
}
