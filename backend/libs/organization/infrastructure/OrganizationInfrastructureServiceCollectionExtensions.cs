using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Organization.Application;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Security;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure;

public static class OrganizationInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationPersistence(this IServiceCollection services, string connectionString)
    {
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddRoomyDbContext<OrganizationDbContext>(connectionString)
            .AddScoped<IOfficeRepository, OfficeRepository>()
            .AddScoped<ICompanyRepository, CompanyRepository>()
            .AddScoped<IEmployeeRepository, EmployeeRepository>()
            .AddScoped<IUnitOfWork, OrganizationUnitOfWork>();

        return services;
    }

    public static IServiceCollection AddOrganizationUseCases(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateOffice, OfficeIdentifier>, CreateOfficeHandler>()
            .AddScoped<ICommandHandler<AddRoomToOffice, RoomIdentifier>, AddRoomToOfficeHandler>()
            .AddScoped<ICommandHandler<RenameOffice>, RenameOfficeHandler>()
            .AddScoped<ICommandHandler<ChangeOfficeLocation>, ChangeOfficeLocationHandler>()
            .AddScoped<ICommandHandler<RenameRoom>, RenameRoomHandler>()
            .AddScoped<ICommandHandler<HireEmployee, HiredEmployee>, HireEmployeeHandler>()
            .AddScoped<ICommandHandler<CompleteEmployeeProvisioning>, CompleteEmployeeProvisioningHandler>()
            .AddScoped<ICommandHandler<FailEmployeeProvisioning>, FailEmployeeProvisioningHandler>()
            .AddScoped<ICommandHandler<RetryEmployeeProvisioning>, RetryEmployeeProvisioningHandler>();

        services.AddScoped<IInitialCredentialEncryptor, CredentialEncryptor>();

        return services;
    }
}
