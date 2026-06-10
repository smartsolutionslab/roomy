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
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// US5 / IA-2 (#27) story-level acceptance: a provisioned employee, logged in via the BFF (the forwarded
// token carries the `employee` realm role), is scoped to employee capabilities. One identity exercises
// both halves of the story's independent test — `/account/me` reports `employee`, and the admin surface
// is forbidden (403, FR-007). The constituent behaviours land in US1 (account/me) and US4 (admin policy
// + realm-role flattening); this pins them together as the employee-login regression guard. The live
// Keycloak + BFF login round-trip is the deferred Playwright e2e (plan.md).
public sealed class EmployeeLoginTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly PostgresDatabaseFixture fixture;
    private readonly WebApplicationFactory<IdentityApiHost> app;

    public EmployeeLoginTests(PostgresDatabaseFixture fixture)
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
                services.RemoveAll<IHostedService>();
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    [Fact]
    public async Task A_logged_in_employee_is_scoped_to_employee_capabilities()
    {
        var subject = await SeedActiveEmployeeAsync();
        var employee = LoggedInEmployee(subject);

        var account = await employee.GetAsync("/account/me", TestContext.Current.CancellationToken);
        account.StatusCode.ShouldBe(HttpStatusCode.OK);
        var projection = await account.Content
            .ReadFromJsonAsync<AccountResponse>(TestContext.Current.CancellationToken);
        projection.ShouldNotBeNull();
        projection.Role.ShouldBe("employee");

        var adminSurface = await employee.GetAsync("/admin/users", TestContext.Current.CancellationToken);
        adminSurface.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<KeycloakSubjectIdentifier> SeedActiveEmployeeAsync()
    {
        var subject = KeycloakSubjectIdentifier.From(Guid.NewGuid());
        var user = User.Register(
            Email.From($"employee-login-{Guid.NewGuid():N}@example.com"),
            DisplayName.From("Test Employee"),
            Role.Employee);
        user.Activate(subject);

        await using var context = fixture.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return subject;
    }

    private HttpClient LoggedInEmployee(KeycloakSubjectIdentifier subject)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject.Value.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "employee");
        return client;
    }

    public void Dispose() => app.Dispose();
}
