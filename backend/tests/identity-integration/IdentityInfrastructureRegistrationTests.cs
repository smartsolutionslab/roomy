using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class IdentityInfrastructureRegistrationTests
{
    [Fact]
    public void Registers_the_user_repository_and_the_keycloak_identity_provider()
    {
        var services = new ServiceCollection();

        services.AddIdentityPersistence("Host=localhost;Database=identity;Username=postgres;Password=postgres");
        services.AddKeycloakIdentityProvider(
            new Uri("http://keycloak.localhost"),
            new KeycloakAdminOptions { AdminUsername = "admin", AdminPassword = "secret" });

        // The integration-event outbox is composed at the host alongside Wolverine (ADR-0037); the unit of
        // work depends on it, so stand in a no-op here to validate the persistence registration in isolation.
        services.AddScoped<IIntegrationEventOutbox, NoopOutbox>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUserRepository>().ShouldBeOfType<UserRepository>();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ShouldBeOfType<IdentityUnitOfWork>();
        scope.ServiceProvider.GetRequiredService<IIdentityProviderPort>().ShouldBeOfType<KeycloakIdentityProvider>();
    }

    [Fact]
    public void Binds_the_register_user_command_handler()
    {
        var services = new ServiceCollection();

        services.AddIdentityUseCases();

        var registration = services.Single(descriptor => descriptor.ServiceType == typeof(ICommandHandler<RegisterUser>));
        registration.ImplementationType.ShouldBe(typeof(RegisterUserHandler));
        registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void Binds_the_grant_administrator_command_handler()
    {
        var services = new ServiceCollection();

        services.AddIdentityUseCases();

        var registration = services.Single(descriptor => descriptor.ServiceType == typeof(ICommandHandler<GrantAdministrator>));
        registration.ImplementationType.ShouldBe(typeof(GrantAdministratorHandler));
        registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    private sealed class NoopOutbox : IIntegrationEventOutbox
    {
        public Task SaveAndPublishAsync(DbContext context, IReadOnlyCollection<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
