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
using SmartSolutionsLab.Roomy.Identity.Api;
using SmartSolutionsLab.Roomy.Identity.Api.Endpoints;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.TestSupport;
using Response = SmartSolutionsLab.Roomy.Identity.Api.Endpoints.Response;
namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// Boots the identity host in-process against real Postgres to verify the admin account surface
// (identity-api.md): listing and reading accounts and the grant-administrator elevation, all
// administrator-only (FR-007). The external infra is removed (no Wolverine, no seeder), the BFF token
// is the test auth scheme, and the Keycloak provider is a recording stub — the real Keycloak round-trip
// is the deferred e2e.
public sealed class AdminUserEndpointsTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly PostgresDatabaseFixture fixture;
    private readonly RecordingIdentityProvider identityProvider = new();
    private readonly WebApplicationFactory<IdentityApiHost> app;

    public AdminUserEndpointsTests(PostgresDatabaseFixture fixture)
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
                services.RemoveAll<IIdentityProviderPort>();
                services.AddSingleton<IIdentityProviderPort>(identityProvider);
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
    }

    private async Task<User> SeedUserAsync(Role role)
    {
        var user = User.Register(
            Email.From($"admin-ep-{Guid.NewGuid():N}@example.com"), DisplayName.From("Test User"), role);
        user.Activate(KeycloakSubjectIdentifier.From(Guid.NewGuid()));

        await using var context = fixture.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private async Task<User> SeedUserWithEmailAsync(string email)
    {
        var user = User.Register(Email.From(email), DisplayName.From("Test User"), Role.Employee);
        user.Activate(KeycloakSubjectIdentifier.From(Guid.NewGuid()));

        await using var context = fixture.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private HttpClient AdministratorClient()
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

    [Fact]
    public async Task Lists_seeded_accounts_with_their_status_for_an_administrator()
    {
        var employee = await SeedUserAsync(Role.Employee);

        var response = await AdministratorClient()
            .GetAsync("/admin/users", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content
            .ReadFromJsonAsync<Response.Page.AdminUser>(TestContext.Current.CancellationToken);
        var listed = page.ShouldNotBeNull().Items.Single(user => user.UserId == employee.Identifier.Value);
        listed.Role.ShouldBe("employee");
        listed.Status.ShouldBe("active");
    }

    [Fact]
    public async Task Walks_every_account_in_email_order_without_skips_or_duplicates()
    {
        var seeded = new List<User>();
        for (var index = 0; index < 5; index++)
        {
            seeded.Add(await SeedUserAsync(Role.Employee));
        }

        var client = AdministratorClient();
        var collected = new List<Response.AdminUser>();
        string? cursor = null;
        var pageCount = 0;
        do
        {
            var url = cursor is null
                ? "/admin/users?limit=2"
                : $"/admin/users?limit=2&cursor={Uri.EscapeDataString(cursor)}";
            var page = await client.GetFromJsonAsync<Response.Page.AdminUser>(url, TestContext.Current.CancellationToken);
            page.ShouldNotBeNull();
            // Every page but the last is full — a page is short only when the list is exhausted, even
            // though other tests' rows interleave with the seeded ones.
            if (page.NextCursor is not null)
            {
                page.Items.Count.ShouldBe(2);
            }

            collected.AddRange(page.Items);
            cursor = page.NextCursor;
            pageCount++;
        }
        while (cursor is not null && pageCount < 1000);

        // Walking to a null cursor with no duplicates and every page full (bar the last) is exactly the
        // keyset guarantee — the server's text collation defines the order, so we assert structure, not
        // a client-side ordinal sort.
        cursor.ShouldBeNull();
        collected.Select(user => user.UserId).ShouldBeUnique();
        foreach (var user in seeded)
        {
            collected.Count(item => item.UserId == user.Identifier.Value).ShouldBe(1);
        }
    }

    [Fact]
    public async Task Paging_is_stable_when_an_account_is_inserted_between_fetches()
    {
        for (var index = 0; index < 3; index++)
        {
            await SeedUserAsync(Role.Employee);
        }

        var client = AdministratorClient();
        var firstPage = await client.GetFromJsonAsync<Response.Page.AdminUser>(
            "/admin/users?limit=2", TestContext.Current.CancellationToken);
        firstPage.ShouldNotBeNull();
        firstPage.NextCursor.ShouldNotBeNull();
        var firstPageIds = firstPage.Items.Select(user => user.UserId).ToList();

        // Insert an account that sorts before the cursor (a "zzz" email would sort after; "aaa" sorts
        // before the first page's last email). Keyset (WHERE email > cursor) must neither re-surface a
        // first-page row nor return this already-passed insert — offset paging would shift and do both.
        var inserted = await SeedUserWithEmailAsync($"aaa-{Guid.NewGuid():N}@example.com");

        var remaining = new List<Response.AdminUser>();
        string? cursor = firstPage.NextCursor;
        while (cursor is not null)
        {
            var page = await client.GetFromJsonAsync<Response.Page.AdminUser>(
                $"/admin/users?limit=2&cursor={Uri.EscapeDataString(cursor)}", TestContext.Current.CancellationToken);
            page.ShouldNotBeNull();
            remaining.AddRange(page.Items);
            cursor = page.NextCursor;
        }

        var remainingIds = remaining.Select(user => user.UserId).ToList();
        remainingIds.ShouldNotContain(firstPageIds[0]);
        remainingIds.ShouldNotContain(firstPageIds[1]);
        remainingIds.ShouldNotContain(inserted.Identifier.Value);
    }

    [Theory]
    [InlineData("/admin/users?limit=0")]
    [InlineData("/admin/users?limit=101")]
    [InlineData("/admin/users?cursor=not-a-valid-cursor")]
    public async Task Rejects_a_bad_page_request_with_400(string url)
    {
        var response = await AdministratorClient().GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_a_single_account_for_an_administrator()
    {
        var employee = await SeedUserAsync(Role.Employee);

        var response = await AdministratorClient()
            .GetAsync($"/admin/users/{employee.Identifier.Value}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content
            .ReadFromJsonAsync<Response.AdminUser>(TestContext.Current.CancellationToken);
        user.ShouldNotBeNull();
        user.Email.ShouldBe(employee.Email.Value);
        user.Role.ShouldBe("employee");
    }

    [Fact]
    public async Task Returns_404_for_an_unknown_account()
    {
        var response = await AdministratorClient()
            .GetAsync($"/admin/users/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Grants_administrator_to_an_employee()
    {
        var employee = await SeedUserAsync(Role.Employee);

        var grant = await AdministratorClient().PostAsync(
            $"/admin/users/{employee.Identifier.Value}:grant-administrator",
            content: null,
            TestContext.Current.CancellationToken);

        grant.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        identityProvider.AssignedSubjects.ShouldContain(employee.KeycloakSubjectIdentifier!.Value);

        await using var context = fixture.CreateContext();
        var reloaded = await context.Users.SingleAsync(
            user => user.Identifier == employee.Identifier, TestContext.Current.CancellationToken);
        reloaded.IsAdministrator.ShouldBeTrue();
    }

    [Fact]
    public async Task Granting_administrator_to_a_provisioning_account_is_422_not_active()
    {
        var provisioning = User.Register(
            Email.From($"prov-{Guid.NewGuid():N}@example.com"), DisplayName.From("Pending"), Role.Employee);
        await using (var context = fixture.CreateContext())
        {
            context.Users.Add(provisioning);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await AdministratorClient().PostAsync(
            $"/admin/users/{provisioning.Identifier.Value}:grant-administrator",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(TestContext.Current.CancellationToken);
        body.ShouldNotBeNull().Code.ShouldBe("user.not_active");
    }

    [Fact]
    public async Task Forbids_an_employee_from_listing_accounts()
    {
        var response = await EmployeeClient()
            .GetAsync("/admin/users", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Forbids_an_employee_from_granting_administrator()
    {
        var employee = await SeedUserAsync(Role.Employee);

        var response = await EmployeeClient().PostAsync(
            $"/admin/users/{employee.Identifier.Value}:grant-administrator",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Requires_a_session_to_list_accounts()
    {
        var response = await app.CreateClient()
            .GetAsync("/admin/users", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    public void Dispose() => app.Dispose();

    private sealed record ErrorDto(string Code, string Message);

    private sealed class RecordingIdentityProvider : IIdentityProviderPort
    {
        public List<KeycloakSubjectIdentifier> AssignedSubjects { get; } = [];

        public Task<Result<KeycloakSubjectIdentifier>> ProvisionUserAsync(
            Email email, DisplayName displayName, string initialPassword, Role role,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("The admin surface does not provision accounts.");

        public Task<Result> AssignAdministratorRoleAsync(
            KeycloakSubjectIdentifier subject, CancellationToken cancellationToken)
        {
            AssignedSubjects.Add(subject);
            return Task.FromResult(Result.Success());
        }
    }
}
