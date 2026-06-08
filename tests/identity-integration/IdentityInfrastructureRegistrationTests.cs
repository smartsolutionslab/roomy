using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// The identity infrastructure registrations are pure DI wiring — no database or Keycloak is contacted
// by registering or resolving them — so this is a fast, container-free check that the composition root
// binds the ports to their adapters.
public sealed class IdentityInfrastructureRegistrationTests
{
    [Fact]
    public void Registers_the_user_repository_and_the_keycloak_identity_provider()
    {
        var services = new ServiceCollection();

        services.AddIdentityPersistence(
            "Host=localhost;Database=identity;Username=postgres;Password=postgres");
        services.AddKeycloakIdentityProvider(
            new Uri("http://keycloak.localhost"),
            new KeycloakAdminOptions { AdminUsername = "admin", AdminPassword = "secret" });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUserRepository>().ShouldBeOfType<UserRepository>();
        scope.ServiceProvider.GetRequiredService<IIdentityProviderPort>()
            .ShouldBeOfType<KeycloakIdentityProvider>();
    }
}
