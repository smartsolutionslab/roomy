using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure;

public static class IdentityInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, string connectionString)
    {
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddRoomyDbContext<IdentityDbContext>(connectionString)
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<IUnitOfWork, IdentityUnitOfWork>();

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    public static IServiceCollection AddKeycloakIdentityProvider(this IServiceCollection services, Uri baseAddress, KeycloakAdminOptions options)
    {
        services.AddSingleton(options)
        .AddHttpClient<IIdentityProviderPort, KeycloakIdentityProvider>(httpClient => httpClient.BaseAddress = baseAddress);

        return services;
    }

    public static IServiceCollection AddIdentityUseCases(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICommandHandler<RegisterUser>, RegisterUserHandler>()
            .AddScoped<ICommandHandler<GrantAdministrator>, GrantAdministratorHandler>();

        return services;
    }
}
