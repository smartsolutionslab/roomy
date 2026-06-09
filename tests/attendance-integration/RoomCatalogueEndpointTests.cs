using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Api;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// Boots the attendance host in-process against the real test Postgres, with the BFF token replaced by
// the test auth scheme. Verifies the GET /rooms contract (007 US1): any session may read it, no session
// is unauthorized, and the catalogue lists the company's seeded rooms with their office names.
public sealed class RoomCatalogueEndpointTests : IClassFixture<PostgresEventStoreFixture>, IDisposable
{
    private static readonly Guid companyId = Guid.Parse("0199a0b0-0000-7000-8000-000000000002");

    private readonly PostgresEventStoreFixture fixture;
    private readonly WebApplicationFactory<AttendanceApiHost> app;

    public RoomCatalogueEndpointTests(PostgresEventStoreFixture fixture)
    {
        this.fixture = fixture;
        app = new WebApplicationFactory<AttendanceApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:attendance", fixture.ConnectionString);
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Attendance:CompanyId", companyId.ToString());

            webHost.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    [Fact]
    public async Task A_request_without_a_session_is_unauthorized()
    {
        var response = await app.CreateClient().GetAsync("/rooms", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task It_returns_the_seeded_bookable_catalogue()
    {
        var officeId = Guid.CreateVersion7();
        var room = Guid.CreateVersion7();

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = companyId, Name = "Munich" });
            seed.Rooms.Add(new Room { RoomId = room, OfficeId = officeId, CompanyId = companyId, Capacity = 8, Name = "A1" });
        });

        var rooms = await Client().GetFromJsonAsync<BookableRoomDto[]>("/rooms", TestContext.Current.CancellationToken);

        rooms.ShouldNotBeNull();
        var listed = rooms.ShouldHaveSingleItem();
        listed.OfficeId.ShouldBe(officeId);
        listed.OfficeName.ShouldBe("Munich");
        listed.RoomId.ShouldBe(room);
        listed.RoomName.ShouldBe("A1");
        listed.Capacity.ShouldBe(8);
    }

    public void Dispose() => app.Dispose();

    private HttpClient Client()
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, Guid.NewGuid().ToString());
        return client;
    }

    private async Task SeedAsync(Action<AttendanceDbContext> seed)
    {
        await using var context = fixture.CreateDbContext();
        seed(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed record BookableRoomDto(Guid OfficeId, string OfficeName, Guid RoomId, string RoomName, int Capacity);
}
