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
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// Boots the attendance host in-process against the real test Postgres, with the BFF token replaced by
// the test auth scheme, a fixed clock (so "today" is deterministic), and a stub room directory (the
// real capacity feed is US2). Verifies the POST /reservations contract (attendance-api.md): the
// Result -> status/code mapping and that the authorization policy enforces 401. Each test uses a
// distinct bookable date so they never share a company-day stream.
public sealed class ReservationEndpointTests : IClassFixture<PostgresEventStoreFixture>, IDisposable
{
    private static readonly DateOnly monday = FirstMondayOnOrAfter(new DateOnly(2026, 6, 1));
    private static readonly DateTimeOffset now = new(monday.Year, monday.Month, monday.Day, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid companyId = Guid.Parse("0199a0b0-0000-7000-8000-000000000001");

    private readonly StubRoomDirectory roomDirectory = new();
    private readonly WebApplicationFactory<AttendanceApiHost> app;

    public ReservationEndpointTests(PostgresEventStoreFixture fixture)
    {
        app = new WebApplicationFactory<AttendanceApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:attendance", fixture.ConnectionString);
            webHost.UseSetting("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672");
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Attendance:CompanyId", companyId.ToString());
            // Skip the RabbitMQ inbox: this test exercises the reservation endpoints only, so the host
            // must not wait on (and time out against) an absent broker during boot.
            webHost.UseSetting("Messaging:Enabled", "false");

            webHost.ConfigureTestServices(services =>
            {
                // Keep the HTTP test free of external infra: drop the Wolverine runtime (it would connect
                // to RabbitMQ on start), and replace the BFF/Keycloak token validation with the test
                // scheme, the live clock with a fixed one, and the room directory with a controllable stub.
                services.RemoveAll<IHostedService>();

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));

                services.RemoveAll<IRoomDirectory>();
                services.AddSingleton<IRoomDirectory>(roomDirectory);

                // Resolve every subject to an employee with the same id, so the acting user owns what it
                // reserves; the admin-on-behalf cases target a different employee id explicitly.
                services.RemoveAll<IEmployeeDirectory>();
                services.AddSingleton<IEmployeeDirectory>(new IdentityEmployeeDirectory());
            });
        });
    }

    [Fact]
    public async Task Reserving_an_available_room_returns_201_with_the_reservation()
    {
        roomDirectory.Capacity = 8;

        var response = await ClientForSubject(Guid.NewGuid())
            .PostAsJsonAsync("/reservations", Booking(monday), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ReservationDto>(TestContext.Current.CancellationToken);
        created.ShouldNotBeNull();
        created.ReservationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task A_full_room_returns_409_room_full()
    {
        roomDirectory.Capacity = 1;
        var date = monday.AddDays(1);
        var room = Guid.NewGuid();
        var office = Guid.NewGuid();

        var first = await ClientForSubject(Guid.NewGuid())
            .PostAsJsonAsync("/reservations", new ReserveBody(office, room, date), TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await ClientForSubject(Guid.NewGuid())
            .PostAsJsonAsync("/reservations", new ReserveBody(office, room, date), TestContext.Current.CancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var error = await second.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        error!.Code.ShouldBe("room_full");
    }

    [Fact]
    public async Task An_unbookable_day_returns_422_not_bookable()
    {
        roomDirectory.Capacity = 8;
        var saturday = monday.AddDays(5);

        var response = await ClientForSubject(Guid.NewGuid())
            .PostAsJsonAsync("/reservations", Booking(saturday), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        error!.Code.ShouldBe("not_bookable");
    }

    [Fact]
    public async Task An_unknown_room_returns_404()
    {
        roomDirectory.Capacity = null; // the room is not known to attendance

        var response = await ClientForSubject(Guid.NewGuid())
            .PostAsJsonAsync("/reservations", Booking(monday.AddDays(2)), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        error!.Code.ShouldBe("unknown_room");
    }

    [Fact]
    public async Task A_request_without_a_session_is_unauthorized()
    {
        var response = await app.CreateClient()
            .PostAsJsonAsync("/reservations", Booking(monday), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cancelling_an_own_reservation_returns_204()
    {
        roomDirectory.Capacity = 8;
        var date = monday.AddDays(3);
        var owner = Guid.NewGuid();
        var reservationId = await CreateReservationAsync(owner, date);

        var response = await ClientForSubject(owner).DeleteAsync(CancelUrl(reservationId, date), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancelling_another_employees_reservation_returns_403()
    {
        roomDirectory.Capacity = 8;
        var date = monday.AddDays(4);
        var reservationId = await CreateReservationAsync(Guid.NewGuid(), date);

        var response = await ClientForSubject(Guid.NewGuid()).DeleteAsync(CancelUrl(reservationId, date), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        error!.Code.ShouldBe("not_owner");
    }

    [Fact]
    public async Task Cancelling_an_unknown_reservation_returns_404()
    {
        var response = await ClientForSubject(Guid.NewGuid())
            .DeleteAsync(CancelUrl(Guid.NewGuid(), monday.AddDays(7)), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        error!.Code.ShouldBe("reservation_not_found");
    }

    [Fact]
    public async Task Reserving_on_behalf_of_another_employee_as_a_non_admin_is_forbidden()
    {
        // FR-011 (scenario 10) — onBehalfOf is administrator-only.
        roomDirectory.Capacity = 8;
        var body = new ReserveBody(Guid.NewGuid(), Guid.NewGuid(), monday.AddDays(8), OnBehalfOf: Guid.NewGuid());

        var response = await ClientForSubject(Guid.NewGuid())
            .PostAsJsonAsync("/reservations", body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        error!.Code.ShouldBe("not_authorized");
    }

    [Fact]
    public async Task An_administrator_reserves_on_behalf_of_another_employee()
    {
        // Scenario 10 — an administrator acts on behalf of an employee; the reservation is the target's.
        roomDirectory.Capacity = 8;
        var target = Guid.NewGuid();
        var body = new ReserveBody(Guid.NewGuid(), Guid.NewGuid(), monday.AddDays(9), OnBehalfOf: target);

        var response = await AdminClientForSubject(Guid.NewGuid())
            .PostAsJsonAsync("/reservations", body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ReservationDto>(TestContext.Current.CancellationToken);
        created!.EmployeeId.ShouldBe(target);
    }

    [Fact]
    public async Task An_administrator_cancels_another_employees_reservation()
    {
        // Scenario 10/11 — an administrator may cancel anyone's reservation.
        roomDirectory.Capacity = 8;
        var date = monday.AddDays(10);
        var reservationId = await CreateReservationAsync(Guid.NewGuid(), date);

        var response = await AdminClientForSubject(Guid.NewGuid())
            .DeleteAsync(CancelUrl(reservationId, date), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Viewing_a_day_returns_its_reservations()
    {
        // Scenario 11 (view) — any authenticated user sees the day's reservations, replayed from the stream.
        roomDirectory.Capacity = 8;
        var date = monday.AddDays(11);
        await CreateReservationAsync(Guid.NewGuid(), date);
        await CreateReservationAsync(Guid.NewGuid(), date);

        var response = await ClientForSubject(Guid.NewGuid())
            .GetAsync(ViewUrl(date), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PageDto<ReservationDto>>(TestContext.Current.CancellationToken);
        page!.Items.Length.ShouldBe(2);
        page.Items.All(reservation => reservation.Date == date).ShouldBeTrue();
        // The day list is bounded by capacity — it adopts the page envelope with no further page (ADR-0044).
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task Viewing_an_empty_day_returns_an_empty_page()
    {
        var response = await ClientForSubject(Guid.NewGuid())
            .GetAsync(ViewUrl(monday.AddDays(12)), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PageDto<ReservationDto>>(TestContext.Current.CancellationToken);
        page!.Items.ShouldBeEmpty();
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task Viewing_my_reservations_lists_my_own_bookings()
    {
        // FR-004 (scenario 6): the caller sees their own reservations. The reserve flow projects into the
        // Reservations read model (ADR-0038), so a freshly booked place appears in "my reservations".
        roomDirectory.Capacity = 8;
        var subject = Guid.NewGuid();
        await CreateReservationAsync(subject, monday.AddDays(7));
        await CreateReservationAsync(subject, monday.AddDays(8));

        var response = await ClientForSubject(subject).GetAsync("/reservations/mine", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mine = await response.Content.ReadFromJsonAsync<PageDto<MyReservationDto>>(TestContext.Current.CancellationToken);
        mine!.Items.Length.ShouldBe(2);
        mine.Items.All(reservation => reservation.Date == monday.AddDays(7) || reservation.Date == monday.AddDays(8)).ShouldBeTrue();
    }

    [Fact]
    public async Task Viewing_my_reservations_excludes_other_employees_bookings()
    {
        roomDirectory.Capacity = 8;
        await CreateReservationAsync(Guid.NewGuid(), monday.AddDays(2));
        var subject = Guid.NewGuid();

        var response = await ClientForSubject(subject).GetAsync("/reservations/mine", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mine = await response.Content.ReadFromJsonAsync<PageDto<MyReservationDto>>(TestContext.Current.CancellationToken);
        mine!.Items.ShouldBeEmpty();
    }

    public void Dispose() => app.Dispose();

    private async Task<Guid> CreateReservationAsync(Guid subject, DateOnly date)
    {
        var response = await ClientForSubject(subject)
            .PostAsJsonAsync("/reservations", Booking(date), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ReservationDto>(TestContext.Current.CancellationToken);
        return created!.ReservationId;
    }

    private static string CancelUrl(Guid reservationId, DateOnly date) =>
        $"/reservations/{reservationId}?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private static string ViewUrl(DateOnly date) =>
        $"/reservations?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private HttpClient ClientForSubject(Guid subject)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject.ToString());
        return client;
    }

    private HttpClient AdminClientForSubject(Guid subject)
    {
        var client = ClientForSubject(subject);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "administrator");
        return client;
    }

    private static ReserveBody Booking(DateOnly date) => new(Guid.NewGuid(), Guid.NewGuid(), date);

    private static DateOnly FirstMondayOnOrAfter(DateOnly start)
    {
        var date = start;
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed record ReserveBody(Guid OfficeId, Guid RoomId, DateOnly Date, Guid? OnBehalfOf = null);

    private sealed record ReservationDto(Guid ReservationId, Guid OfficeId, Guid RoomId, DateOnly Date, Guid EmployeeId);

    private sealed record MyReservationDto(Guid ReservationId, Guid OfficeId, string OfficeName, Guid RoomId, string RoomName, DateOnly Date);

    private sealed record PageDto<T>(T[] Items, string? NextCursor);

    private sealed record ErrorDto(string Code, string Message);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class IdentityEmployeeDirectory : IEmployeeDirectory
    {
        public Task<Result<EmployeeIdentifier>> FindByUserAsync(UserIdentifier user, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(EmployeeIdentifier.From(user.Value)));
    }

    private sealed class StubRoomDirectory : IRoomDirectory
    {
        public int? Capacity { get; set; } = 8;

        public Task<Result<RoomCapacity>> FindCapacityAsync(RoomIdentifier room, CancellationToken cancellationToken) =>
            Task.FromResult(Capacity is null
                ? Result.Failure<RoomCapacity>(Error.NotFound("unknown_room", "The room is not known."))
                : Result.Success(RoomCapacity.From(Capacity.Value)));
    }
}
