using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Api;
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

// Boots the organization host in-process against the real test Postgres, with the BFF token replaced by
// the test auth scheme, to verify the office endpoints and their authorization (organization-api.md).
// The company seeder runs at startup, so CreateOffice has a company to create offices under.
public sealed class OfficeEndpointsTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly WebApplicationFactory<OrganizationApiHost> app;

    public OfficeEndpointsTests(PostgresDatabaseFixture fixture)
    {
        app = new WebApplicationFactory<OrganizationApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:organization", fixture.ConnectionString);
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Company:Name", "Roomy Test Company");

            webHost.ConfigureTestServices(services =>
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
        });
    }

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

    private static CreateOfficeRequest OfficeNamed(string name) => new(name, "Berlin");

    [Fact]
    public async Task An_administrator_creates_an_office()
    {
        var response = await ClientWithRoles("employee", "administrator")
            .PostAsJsonAsync("/offices", OfficeNamed($"HQ-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var office = await response.Content
            .ReadFromJsonAsync<OfficeResponse>(TestContext.Current.CancellationToken);
        office.ShouldNotBeNull();
        office.Capacity.ShouldBe(0);
        office.Rooms.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_employee_cannot_create_an_office()
    {
        var response = await ClientWithRoles("employee")
            .PostAsJsonAsync("/offices", OfficeNamed("Forbidden"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_unauthenticated_request_is_rejected()
    {
        var response = await app.CreateClient()
            .PostAsJsonAsync("/offices", OfficeNamed("NoSession"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_blank_name_is_rejected()
    {
        var response = await ClientWithRoles("administrator")
            .PostAsJsonAsync("/offices", new CreateOfficeRequest("   ", "Berlin"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_duplicate_office_name_is_rejected()
    {
        var name = $"Duplicate-{Guid.NewGuid():N}";
        var administrator = ClientWithRoles("administrator");

        (await administrator.PostAsJsonAsync("/offices", OfficeNamed(name), TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await administrator
            .PostAsJsonAsync("/offices", OfficeNamed(name), TestContext.Current.CancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Created_offices_are_listed_and_fetchable_by_id()
    {
        var administrator = ClientWithRoles("administrator");
        var created = await administrator
            .PostAsJsonAsync("/offices", OfficeNamed($"Listed-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        var office = await created.Content
            .ReadFromJsonAsync<OfficeResponse>(TestContext.Current.CancellationToken);
        office.ShouldNotBeNull();

        var byId = await administrator.GetAsync($"/offices/{office.Id}", TestContext.Current.CancellationToken);
        byId.StatusCode.ShouldBe(HttpStatusCode.OK);

        var all = await administrator
            .GetFromJsonAsync<OfficeResponse[]>("/offices", TestContext.Current.CancellationToken);
        all.ShouldNotBeNull();
        all.ShouldContain(entry => entry.Id == office.Id);
    }

    public void Dispose() => app.Dispose();
}
