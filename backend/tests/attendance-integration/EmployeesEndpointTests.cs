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

public sealed class EmployeesEndpointTests : IClassFixture<PostgresEventStoreFixture>, IDisposable
{
    private static readonly Guid companyId = Guid.Parse("0199a0b0-0000-7000-8000-000000000003");

    private readonly PostgresEventStoreFixture fixture;
    private readonly WebApplicationFactory<AttendanceApiHost> app;

    public EmployeesEndpointTests(PostgresEventStoreFixture fixture)
    {
        this.fixture = fixture;
        app = new WebApplicationFactory<AttendanceApiHost>().WithWebHostBuilder(webHost =>
        {
            webHost.UseSetting("ConnectionStrings:attendance", fixture.ConnectionString);
            webHost.UseSetting("Keycloak:BaseAddress", "http://keycloak.localhost");
            webHost.UseSetting("Keycloak:Realm", "roomy");
            webHost.UseSetting("Attendance:CompanyId", companyId.ToString());
            webHost.UseSetting("Messaging:Enabled", "false");

            webHost.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddAuthentication(TestAuthHandler.SchemeName).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    [Fact]
    public async Task The_directory_without_a_session_is_unauthorized()
    {
        var response = await app.CreateClient().GetAsync("/reservations/employees", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_non_administrator_is_forbidden_from_the_directory()
    {
        var response = await EmployeeClient().GetAsync("/reservations/employees", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_administrator_gets_the_employee_directory()
    {
        var employee = Guid.CreateVersion7();
        await SeedAsync(seed =>
            seed.Employees.Add(new Employee { EmployeeId = employee, UserId = Guid.CreateVersion7(), DisplayName = "Ada" }));

        var page = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>("/reservations/employees", TestContext.Current.CancellationToken);

        page.ShouldNotBeNull();
        page.Items.ShouldContain(candidate => candidate.EmployeeId == employee && candidate.Name == "Ada");
    }

    [Fact]
    public async Task The_employee_directory_rejects_a_bad_page_request_with_400()
    {
        var response = await AdminClient().GetAsync("/reservations/employees?limit=0", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_single_typo_query_returns_the_intended_employee_on_the_first_page()
    {
        await SeedCorpusAsync();

        var page = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>(
            "/reservations/employees?q=" + EmployeeNameSamples.TypoQuery, TestContext.Current.CancellationToken);

        page.ShouldNotBeNull();
        page.Items.ShouldContain(candidate => candidate.Name == EmployeeNameSamples.TypoTarget);
    }

    [Fact]
    public async Task Search_returns_only_similar_names_ranked_most_similar_first()
    {
        await SeedCorpusAsync();

        var page = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>("/reservations/employees?q=" + EmployeeNameSamples.TypoQuery, TestContext.Current.CancellationToken);

        page.ShouldNotBeNull();
        var names = page.Items.Select(employee => employee.Name).ToList();

        names.ShouldNotContain("Ada Lovelace");

        names.ShouldContain(EmployeeNameSamples.TypoTarget);
        names.ShouldContain(EmployeeNameSamples.LooserTypoMatch);
        names.IndexOf(EmployeeNameSamples.TypoTarget).ShouldBeLessThan(names.IndexOf(EmployeeNameSamples.LooserTypoMatch));
    }

    [Fact]
    public async Task Search_folds_accents_so_an_unaccented_query_finds_an_accented_name()
    {
        await SeedCorpusAsync();

        var byGivenName = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>("/reservations/employees?q=" + EmployeeNameSamples.AccentStrippedGivenNameQuery,
            TestContext.Current.CancellationToken);
        var bySurname = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>("/reservations/employees?q=" + EmployeeNameSamples.AccentStrippedSurnameQuery,
            TestContext.Current.CancellationToken);

        byGivenName.ShouldNotBeNull();
        bySurname.ShouldNotBeNull();
        byGivenName.Items.ShouldContain(candidate => candidate.Name == EmployeeNameSamples.AccentedTarget);
        bySurname.Items.ShouldContain(candidate => candidate.Name == EmployeeNameSamples.AccentedTarget);
    }

    [Fact]
    public async Task Search_paging_is_stable_when_a_matching_employee_is_inserted_mid_scroll()
    {
        var surname = $"Zylit{Guid.NewGuid():N}";
        var family = new[] { "Aaron", "Bea", "Cora", "Dan", "Eve" }
            .Select(given => (Id: Guid.CreateVersion7(), Name: $"{given} {surname}"))
            .ToList();
        await SeedAsync(seed =>
        {
            foreach (var member in family)
            {
                seed.Employees.Add(new Employee { EmployeeId = member.Id, UserId = Guid.CreateVersion7(), DisplayName = member.Name });
            }
        });

        var seen = new List<Guid>();
        var firstPage = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>($"/reservations/employees?q={surname}&limit=2", TestContext.Current.CancellationToken);
        firstPage.ShouldNotBeNull();
        firstPage.Items.Length.ShouldBe(2);
        firstPage.NextCursor.ShouldNotBeNull();
        seen.AddRange(firstPage.Items.Select(employee => employee.EmployeeId));

        await SeedAsync(seed =>
            seed.Employees.Add(new Employee { EmployeeId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7(), DisplayName = $"Zoe {surname}" }));

        var cursor = firstPage.NextCursor;
        var guard = 0;
        while (cursor is not null && guard++ < 100)
        {
            var next = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>($"/reservations/employees?q={surname}&limit=2&cursor={Uri.EscapeDataString(cursor)}",
                TestContext.Current.CancellationToken);
            next.ShouldNotBeNull();
            seen.AddRange(next.Items.Select(employee => employee.EmployeeId));
            cursor = next.NextCursor;
        }

        seen.ShouldBeUnique();
        foreach (var member in family)
        {
            seen.ShouldContain(member.Id);
        }
    }

    [Fact]
    public async Task A_blank_query_returns_the_same_unfiltered_list_as_no_query()
    {
        await SeedAsync(seed =>
        {
            seed.Employees.Add(new Employee { EmployeeId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7(), DisplayName = "Mara" });
            seed.Employees.Add(new Employee { EmployeeId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7(), DisplayName = "Nadia" });
        });

        var withoutQuery = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>("/reservations/employees?limit=100", TestContext.Current.CancellationToken);
        var withBlankQuery = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>("/reservations/employees?q=%20%20&limit=100", TestContext.Current.CancellationToken);

        withoutQuery.ShouldNotBeNull();
        withBlankQuery.ShouldNotBeNull();
        withBlankQuery.Items.Select(employee => employee.EmployeeId).ShouldBe(withoutQuery.Items.Select(employee => employee.EmployeeId));
    }

    [Fact]
    public async Task A_query_longer_than_the_maximum_is_rejected_with_400()
    {
        var overLong = new string('a', 101);

        var response = await AdminClient().GetAsync("/reservations/employees?q=" + overLong, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_cursor_issued_for_the_unfiltered_list_is_rejected_when_replayed_with_a_query()
    {
        await SeedCorpusAsync();

        var unfiltered = await AdminClient().GetFromJsonAsync<PageDto<EmployeeDto>>("/reservations/employees?limit=1", TestContext.Current.CancellationToken);
        unfiltered.ShouldNotBeNull();
        unfiltered.NextCursor.ShouldNotBeNull();

        var response = await AdminClient().GetAsync($"/reservations/employees?q=Hannah&cursor={Uri.EscapeDataString(unfiltered.NextCursor)}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_non_administrator_is_forbidden_from_the_directory_with_a_query()
    {
        var response = await EmployeeClient().GetAsync("/reservations/employees?q=Hannah", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_non_administrator_is_forbidden_from_an_employees_reservations()
    {
        var response = await EmployeeClient().GetAsync("/reservations/by-employee/" + Guid.NewGuid(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_administrator_gets_an_employees_reservations()
    {
        var employee = Guid.CreateVersion7();
        var officeId = Guid.CreateVersion7();
        var room = Guid.CreateVersion7();
        var reservation = Guid.CreateVersion7();
        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = companyId, Name = "Munich" });
            seed.Rooms.Add(new Room { RoomId = room, OfficeId = officeId, CompanyId = companyId, Capacity = 8, Name = "A1" });
            seed.Reservations.Add(new ReservationRow
            {
                ReservationId = reservation,
                CompanyId = companyId,
                EmployeeId = employee,
                OfficeId = officeId,
                RoomId = room,
                Date = new DateOnly(2026, 6, 10),
            });
        });

        var page = await AdminClient().GetFromJsonAsync<PageDto<MyReservationDto>>("/reservations/by-employee/" + employee, TestContext.Current.CancellationToken);

        page.ShouldNotBeNull();
        var listed = page.Items.ShouldHaveSingleItem();
        listed.ReservationId.ShouldBe(reservation);
        listed.RoomName.ShouldBe("A1");
    }

    public void Dispose() => app.Dispose();

    private HttpClient AdminClient()
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "administrator");
        return client;
    }

    private HttpClient EmployeeClient()
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

    private Task SeedCorpusAsync() =>
        SeedAsync(seed =>
        {
            foreach (var name in EmployeeNameSamples.Corpus)
            {
                seed.Employees.Add(new Employee { EmployeeId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7(), DisplayName = name });
            }
        });

    private sealed record EmployeeDto(
        Guid EmployeeId,
        string Name);

    private sealed record MyReservationDto(
        Guid ReservationId,
        Guid OfficeId,
        string OfficeName,
        Guid RoomId,
        string RoomName,
        DateOnly Date);

    private sealed record PageDto<T>(T[] Items, string? NextCursor);
}
