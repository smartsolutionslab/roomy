using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Api;
using SmartSolutionsLab.Roomy.Identity.Api.Endpoints;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// Boots the identity host in-process against the real test Postgres, with the external infra removed
// (no Wolverine runtime, no Keycloak seeder) and the BFF token replaced by the test auth scheme, to
// verify the GET /account/me contract (identity-api.md).
public sealed class AccountMeTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly PostgresDatabaseFixture fixture;
    private readonly WebApplicationFactory<IdentityApiHost> app;

    public AccountMeTests(PostgresDatabaseFixture fixture)
    {
        this.fixture = fixture;
        app = new WebApplicationFactory<IdentityApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:identity", fixture.ConnectionString);
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:AdminUsername", "admin");
            webHost.UseSetting("Keycloak:AdminPassword", "admin");
            webHost.UseSetting("DefaultAdmin:Email", "default-admin@roomy.test");
            webHost.UseSetting("DefaultAdmin:DisplayName", "Default Admin");
            webHost.UseSetting("DefaultAdmin:InitialPassword", "default-admin-password");

            webHost.ConfigureTestServices(services =>
            {
                // Keep the HTTP test free of external infra: drop the Wolverine runtime and the
                // DefaultAdmin seeder (both connect on start), and authenticate via the test scheme
                // instead of validating a real Keycloak token.
                services.RemoveAll<IHostedService>();
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    private async Task<KeycloakSubjectIdentifier> SeedActiveUserAsync(Email email, Role role)
    {
        var subject = KeycloakSubjectIdentifier.From(Guid.NewGuid());
        var user = User.Register(email, DisplayName.From("Test User"), role);
        user.Activate(subject);

        await using var context = fixture.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return subject;
    }

    private HttpClient ClientForSubject(KeycloakSubjectIdentifier subject)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject.Value.ToString());
        return client;
    }

    [Fact]
    public async Task Returns_the_administrator_projection_for_an_authenticated_admin()
    {
        var subject = await SeedActiveUserAsync(
            Email.From($"me-admin-{Guid.NewGuid():N}@example.com"),
            Role.Employee.GrantAdministrator());

        var response = await ClientForSubject(subject)
            .GetAsync("/account/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>(TestContext.Current.CancellationToken);
        account.ShouldNotBeNull();
        account.Role.ShouldBe("administrator");
    }

    [Fact]
    public async Task Returns_the_employee_projection_for_an_authenticated_employee()
    {
        var subject = await SeedActiveUserAsync(
            Email.From($"me-employee-{Guid.NewGuid():N}@example.com"),
            Role.Employee);

        var response = await ClientForSubject(subject)
            .GetAsync("/account/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>(TestContext.Current.CancellationToken);
        account.ShouldNotBeNull();
        account.Role.ShouldBe("employee");
    }

    [Fact]
    public async Task Returns_401_without_a_session()
    {
        var response = await app.CreateClient()
            .GetAsync("/account/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_when_the_authenticated_subject_has_no_account()
    {
        var response = await ClientForSubject(KeycloakSubjectIdentifier.From(Guid.NewGuid()))
            .GetAsync("/account/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public void Dispose() => app.Dispose();
}
