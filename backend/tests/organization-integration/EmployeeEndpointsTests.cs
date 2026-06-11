using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Api;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class EmployeeEndpointsTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly WebApplicationFactory<OrganizationApiHost> app;

    public EmployeeEndpointsTests(PostgresDatabaseFixture fixture)
    {
        SeedCompany(fixture);

        app = new WebApplicationFactory<OrganizationApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:organization", fixture.ConnectionString);
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Company:Name", "Roomy Test Company");

            webHost.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IIntegrationEventOutbox>();
                services.AddScoped<IIntegrationEventOutbox, SavingOnlyOutbox>();
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    [Fact]
    public async Task An_administrator_hires_a_colleague_and_gets_202_provisioning()
    {
        var response = await ClientWithRoles("administrator")
            .PostAsJsonAsync("/employees", Hire("ada@example.com"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var hired = await response.Content.ReadFromJsonAsync<HiredEmployeeDto>(TestContext.Current.CancellationToken);
        hired.ShouldNotBeNull();
        hired.EmployeeId.ShouldNotBe(Guid.Empty);
        hired.UserId.ShouldNotBe(Guid.Empty);
        hired.State.ShouldBe("Provisioning");
    }

    [Fact]
    public async Task A_non_administrator_is_forbidden()
    {
        var response = await ClientWithRoles()
            .PostAsJsonAsync("/employees", Hire("bob@example.com"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_request_without_a_session_is_unauthorized()
    {
        var response = await app.CreateClient()
            .PostAsJsonAsync("/employees", Hire("eve@example.com"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "ada@example.com", "Employee", "pw")]
    [InlineData("Ada", "not-an-email", "Employee", "pw")]
    [InlineData("Ada", "ada@example.com", "Wizard", "pw")]
    [InlineData("Ada", "ada@example.com", "Employee", "")]
    public async Task An_invalid_hire_is_rejected_with_400(string name, string email, string role, string password)
    {
        var response = await ClientWithRoles("administrator").PostAsJsonAsync(
            "/employees",
            new HireDto(name, email, role, password),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    public void Dispose() => app.Dispose();

    private static HireDto Hire(string email) => new("Ada Lovelace", email, "Employee", "transient-pw");

    private HttpClient ClientWithRoles(params string[] roles)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, Guid.NewGuid().ToString());
        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        }

        return client;
    }

    private static void SeedCompany(PostgresDatabaseFixture fixture)
    {
        using var context = fixture.CreateContext();
        if (context.Set<Company>().Any())
        {
            return;
        }

        context.Add(Company.Create(CompanyName.From("Roomy Test Company")));
        context.SaveChanges();
    }

    private sealed class SavingOnlyOutbox : IIntegrationEventOutbox
    {
        public Task SaveAndPublishAsync(
            DbContext context,
            IReadOnlyCollection<IIntegrationEvent> integrationEvents,
            CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
    }

    private sealed record HireDto(string DisplayName, string Email, string Role, string InitialPassword);

    private sealed record HiredEmployeeDto(Guid EmployeeId, Guid UserId, string State);
}
