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
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints;
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Request;
using SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Response;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.TestSupport;
namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

// Boots the organization host in-process against the real test Postgres, with the BFF token replaced by
// the test auth scheme, to verify the office endpoints and their authorization (organization-api.md).
// The Wolverine runtime is dropped (it would connect to RabbitMQ on start, ADR-0037), so the company the
// seeder would create is seeded here instead, giving CreateOffice a company to create offices under.
public sealed class OfficeEndpointsTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly WebApplicationFactory<OrganizationApiHost> app;

    public OfficeEndpointsTests(PostgresDatabaseFixture fixture)
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
                // Keep the HTTP test free of external infra: drop the Wolverine runtime (and the company
                // seeder, replaced by SeedCompany above), replace the Wolverine outbox with one that just
                // saves (the drain is covered by OrganizationUnitOfWorkTests), and use the test auth scheme.
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IIntegrationEventOutbox>();
                services.AddScoped<IIntegrationEventOutbox, SavingOnlyOutbox>();
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
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

    // Saves the work but skips the Wolverine relay (removed with the runtime above); these tests assert
    // the HTTP/office behaviour, while OrganizationUnitOfWorkTests covers the domain-event drain.
    private sealed class SavingOnlyOutbox : IIntegrationEventOutbox
    {
        public Task SaveAndPublishAsync(
            DbContext context,
            IReadOnlyCollection<IIntegrationEvent> integrationEvents,
            CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
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
        var body = await second.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        body.ShouldNotBeNull().Code.ShouldNotBeNullOrEmpty();
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

    private async Task<OfficeResponse> CreateOfficeAsync(HttpClient client, string name)
    {
        var response = await client
            .PostAsJsonAsync("/offices", OfficeNamed(name), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var office = await response.Content
            .ReadFromJsonAsync<OfficeResponse>(TestContext.Current.CancellationToken);
        office.ShouldNotBeNull();
        return office;
    }

    [Fact]
    public async Task An_administrator_adds_a_room_and_the_office_capacity_reflects_it()
    {
        var administrator = ClientWithRoles("administrator");
        var office = await CreateOfficeAsync(administrator, $"WithRooms-{Guid.NewGuid():N}");

        var response = await administrator.PostAsJsonAsync(
            $"/offices/{office.Id}/rooms",
            new AddRoomRequest("Aurora", 8),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var room = await response.Content.ReadFromJsonAsync<RoomResponse>(TestContext.Current.CancellationToken);
        room.ShouldNotBeNull();
        room.Capacity.ShouldBe(8);

        var reloaded = await administrator
            .GetFromJsonAsync<OfficeResponse>($"/offices/{office.Id}", TestContext.Current.CancellationToken);
        reloaded.ShouldNotBeNull();
        reloaded.Capacity.ShouldBe(8);
        reloaded.Rooms.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_room_capacity_below_one_is_rejected()
    {
        var administrator = ClientWithRoles("administrator");
        var office = await CreateOfficeAsync(administrator, $"BadCapacity-{Guid.NewGuid():N}");

        var response = await administrator.PostAsJsonAsync(
            $"/offices/{office.Id}/rooms",
            new AddRoomRequest("Aurora", 0),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_duplicate_room_name_is_rejected()
    {
        var administrator = ClientWithRoles("administrator");
        var office = await CreateOfficeAsync(administrator, $"DupRoom-{Guid.NewGuid():N}");
        await administrator.PostAsJsonAsync(
            $"/offices/{office.Id}/rooms", new AddRoomRequest("Aurora", 8), TestContext.Current.CancellationToken);

        var response = await administrator.PostAsJsonAsync(
            $"/offices/{office.Id}/rooms", new AddRoomRequest("Aurora", 4), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_employee_cannot_add_a_room()
    {
        var office = await CreateOfficeAsync(ClientWithRoles("administrator"), $"RoomAuthz-{Guid.NewGuid():N}");

        var response = await ClientWithRoles("employee").PostAsJsonAsync(
            $"/offices/{office.Id}/rooms", new AddRoomRequest("Aurora", 8), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_administrator_renames_an_office()
    {
        var administrator = ClientWithRoles("administrator");
        var office = await CreateOfficeAsync(administrator, $"Rename-{Guid.NewGuid():N}");
        var newName = $"Renamed-{Guid.NewGuid():N}";

        var response = await administrator.PatchAsJsonAsync(
            $"/offices/{office.Id}/name", new RenameOfficeRequest(newName), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<OfficeResponse>(TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe(newName);
    }

    [Fact]
    public async Task Renaming_an_office_to_an_existing_name_is_rejected()
    {
        var administrator = ClientWithRoles("administrator");
        var taken = await CreateOfficeAsync(administrator, $"Taken-{Guid.NewGuid():N}");
        var other = await CreateOfficeAsync(administrator, $"Other-{Guid.NewGuid():N}");

        var response = await administrator.PatchAsJsonAsync(
            $"/offices/{other.Id}/name", new RenameOfficeRequest(taken.Name), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_administrator_changes_an_office_location()
    {
        var administrator = ClientWithRoles("administrator");
        var office = await CreateOfficeAsync(administrator, $"Relocate-{Guid.NewGuid():N}");

        var response = await administrator.PatchAsJsonAsync(
            $"/offices/{office.Id}/location", new RelocateOfficeRequest("Munich"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<OfficeResponse>(TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.Location.ShouldBe("Munich");
    }

    [Fact]
    public async Task An_administrator_renames_a_room()
    {
        var administrator = ClientWithRoles("administrator");
        var office = await CreateOfficeAsync(administrator, $"RenameRoom-{Guid.NewGuid():N}");
        var added = await administrator.PostAsJsonAsync(
            $"/offices/{office.Id}/rooms", new AddRoomRequest("Aurora", 8), TestContext.Current.CancellationToken);
        var room = await added.Content.ReadFromJsonAsync<RoomResponse>(TestContext.Current.CancellationToken);
        room.ShouldNotBeNull();

        var response = await administrator.PatchAsJsonAsync(
            $"/offices/{office.Id}/rooms/{room.Id}/name",
            new RenameRoomRequest("Polaris"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<OfficeResponse>(TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.Rooms.ShouldContain(entry => entry.Name == "Polaris");
    }

    [Fact]
    public async Task Renaming_an_unknown_room_is_not_found()
    {
        var administrator = ClientWithRoles("administrator");
        var office = await CreateOfficeAsync(administrator, $"NoRoom-{Guid.NewGuid():N}");

        var response = await administrator.PatchAsJsonAsync(
            $"/offices/{office.Id}/rooms/{Guid.NewGuid()}/name",
            new RenameRoomRequest("Polaris"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        body.ShouldNotBeNull().Code.ShouldNotBeNullOrEmpty();
    }

    public void Dispose() => app.Dispose();

    private sealed record ErrorDto(string Code, string Message);
}
