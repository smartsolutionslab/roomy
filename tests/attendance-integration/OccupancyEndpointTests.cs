using System.Globalization;
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
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.TestSupport;
using ReservationRow = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations.Reservation;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// Boots the attendance host in-process against the real test Postgres, with the BFF token replaced by
// the test auth scheme and a fixed clock (so "today" is deterministic). Verifies the GET /occupancy
// contract (attendance-api.md, 004 US6): scope/range validation, the unknown-office 404, and that the
// figures are returned with occupants present only for today/tomorrow (FR-007). The occupancy read model
// is the real one over the seeded read-model tables.
public sealed class OccupancyEndpointTests : IClassFixture<PostgresEventStoreFixture>, IDisposable
{
    private static readonly DateOnly today = new(2026, 6, 8);
    private static readonly DateTimeOffset now = new(2026, 6, 8, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid companyId = Guid.Parse("0199a0b0-0000-7000-8000-000000000001");

    private readonly PostgresEventStoreFixture fixture;
    private readonly WebApplicationFactory<AttendanceApiHost> app;

    public OccupancyEndpointTests(PostgresEventStoreFixture fixture)
    {
        this.fixture = fixture;
        app = new WebApplicationFactory<AttendanceApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:attendance", fixture.ConnectionString);
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Attendance:CompanyId", companyId.ToString());
            // Skip the RabbitMQ inbox: this test exercises the read side only, so the host must not wait
            // on (and time out against) an absent broker during boot.
            webHost.UseSetting("Messaging:Enabled", "false");

            webHost.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
            });
        });
    }

    [Fact]
    public async Task A_request_without_a_session_is_unauthorized()
    {
        var response = await app.CreateClient().GetAsync("/occupancy?officeId=" + Guid.NewGuid(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Neither_office_nor_room_returns_422_unknown_scope()
    {
        var response = await Client().GetAsync("/occupancy", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await Error(response)).Code.ShouldBe("unknown_scope");
    }

    [Fact]
    public async Task Both_office_and_room_returns_422_unknown_scope()
    {
        var response = await Client().GetAsync(
            $"/occupancy?officeId={Guid.NewGuid()}&roomId={Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await Error(response)).Code.ShouldBe("unknown_scope");
    }

    [Fact]
    public async Task A_range_beyond_the_bound_returns_422_range_too_large()
    {
        var to = today.AddMonths(3).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var from = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var response = await Client().GetAsync(
            $"/occupancy?officeId={Guid.NewGuid()}&from={from}&to={to}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await Error(response)).Code.ShouldBe("range_too_large");
    }

    [Fact]
    public async Task An_unknown_office_returns_404()
    {
        var response = await Client().GetAsync("/occupancy?officeId=" + Guid.NewGuid(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Error(response)).Code.ShouldBe("unknown_office");
    }

    [Fact]
    public async Task A_seeded_office_returns_figures_with_names_only_for_today()
    {
        var officeId = Guid.CreateVersion7();
        var room = Guid.CreateVersion7();
        var employee = Guid.CreateVersion7();
        var laterDay = today.AddDays(3);

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = companyId, Name = "Munich" });
            seed.Rooms.Add(new Room { RoomId = room, OfficeId = officeId, CompanyId = companyId, Capacity = 8, Name = "A1" });
            seed.Employees.Add(new Employee { EmployeeId = employee, UserId = Guid.CreateVersion7(), DisplayName = "Ada" });
            seed.Reservations.Add(Reservation(employee, officeId, room, today));
            seed.Reservations.Add(Reservation(employee, officeId, room, laterDay));
        });

        var url = $"/occupancy?officeId={officeId}"
            + $"&from={today:yyyy-MM-dd}&to={laterDay:yyyy-MM-dd}";
        var days = await Client().GetFromJsonAsync<DayDto[]>(url, TestContext.Current.CancellationToken);

        days.ShouldNotBeNull();
        var todayDay = days.Single(day => day.Date == today);
        todayDay.Office.Occupied.ShouldBe(1);
        todayDay.Office.Capacity.ShouldBe(8);
        todayDay.Rooms.Single().Occupants.ShouldNotBeNull();
        todayDay.Rooms.Single().Occupants!.Single().Name.ShouldBe("Ada");

        // Three days out: the count is reported but the names are withheld (absent from the payload).
        var later = days.Single(day => day.Date == laterDay);
        later.Rooms.Single().Occupied.ShouldBe(1);
        later.Rooms.Single().Occupants.ShouldBeNull();
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

    private static ReservationRow Reservation(Guid employee, Guid officeId, Guid room, DateOnly date) =>
        new()
        {
            ReservationId = Guid.CreateVersion7(),
            CompanyId = companyId,
            EmployeeId = employee,
            OfficeId = officeId,
            RoomId = room,
            Date = date,
        };

    private static async Task<ErrorDto> Error(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ErrorDto>())!;

    private sealed record DayDto(DateOnly Date, OfficeDto Office, IReadOnlyList<RoomDto> Rooms);

    private sealed record OfficeDto(Guid OfficeId, string Name, int Occupied, int Capacity, bool IsFull);

    private sealed record RoomDto(Guid RoomId, string Name, int Occupied, int Capacity, bool IsFull, IReadOnlyList<OccupantDto>? Occupants);

    private sealed record OccupantDto(Guid EmployeeId, string Name);

    private sealed record ErrorDto(string Code, string Message);
}
