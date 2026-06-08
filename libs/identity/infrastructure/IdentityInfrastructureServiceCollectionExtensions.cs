using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure;

// Composition-root wiring for the identity context's infrastructure adapters. Keeps the EF Core and
// Keycloak registration details out of the host's Program.cs so the host reads as a list of
// capabilities (ADR-0003/0012/0013). The messaging backbone (Wolverine outbox) is wired separately by
// AddRoomyMessaging.
public static class IdentityInfrastructureServiceCollectionExtensions
{
    // Registers the identity database (its own Postgres, ADR-0014) and the User repository over it.
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddRoomyDbContext<IdentityDbContext>(connectionString);
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    // Registers the Keycloak admin adapter behind the identity-provider port (ADR-0013). The adapter
    // is a typed HttpClient so it gets pooled handlers and the shared resilience defaults; the admin
    // options carry the realm and credentials resolved from configuration at the composition root.
    public static IServiceCollection AddKeycloakIdentityProvider(
        this IServiceCollection services,
        Uri baseAddress,
        KeycloakAdminOptions options)
    {
        Ensure.That((Uri?)baseAddress).IsNotNull();
        Ensure.That((KeycloakAdminOptions?)options).IsNotNull();

        services.AddSingleton(options);
        services.AddHttpClient<IIdentityProviderPort, KeycloakIdentityProvider>(
            httpClient => httpClient.BaseAddress = baseAddress);

        return services;
    }
}
