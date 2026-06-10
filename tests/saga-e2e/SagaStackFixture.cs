using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Saga.E2ETests;

// Boots the full provisioning-saga stack once for the test class (ADR-0025): Postgres, RabbitMQ, real
// Keycloak (production realm import), the schema runner, and both saga participants (identity-api,
// organization-api). It enables direct-access grants on the roomy-bff client at runtime so a test can
// mint a real user token, and exposes helpers to call organization-api, acquire tokens, and poll an
// employee's provisioning state from organization's own database. Requires Docker; first run is slow
// (Keycloak import + migrations + two hosts).
public sealed class SagaStackFixture : IAsyncLifetime
{
    private const string Realm = "roomy";
    private const string BffClientId = "roomy-bff";
    private const string BffClientSecret = "dev-only-bff-secret-change-me";
    private const string MasterAdminUsername = "admin";
    private const string MasterAdminPassword = "admin-test-only-password";

    public const string AdminEmail = "admin@roomy.local";
    public const string AdminPassword = "DevAdmin.23456";

    private DistributedApplication? application;
    private HttpClient? keycloak;
    private string organizationConnectionString = string.Empty;

    public HttpClient Organization { get; private set; } = new();

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Roomy_Saga_TestAppHost>();
        application = await builder.BuildAsync();
        await application.StartAsync();

        // Keycloak import + migrations + two hosts; allow generously.
        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var notifications = application.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync("organization-api", readiness.Token);
        await notifications.WaitForResourceHealthyAsync("identity-api", readiness.Token);

        // A host reports healthy before its Wolverine runtime has finished binding its RabbitMQ listener
        // queues. Publishing before the consumer's queue exists drops the message (the durable outbox
        // considers it sent once the broker accepts the publish to the exchange). Production runs both
        // services continuously, so this only bites at cold start — settle so both sides' listeners bind.
        await Task.Delay(TimeSpan.FromSeconds(30), readiness.Token);

        Organization = new HttpClient { BaseAddress = Endpoint("organization-api") };
        keycloak = new HttpClient { BaseAddress = Endpoint("keycloak") };
        organizationConnectionString = await application.GetConnectionStringAsync("organization", readiness.Token)
            ?? throw new InvalidOperationException("No organization connection string.");

        await EnableDirectAccessGrantsAsync(readiness.Token);
    }

    // A bearer token for a user via the BFF client's direct grant (enabled for the test). The DefaultAdmin
    // is seeded asynchronously at identity startup, so acquiring its token is retried until it appears.
    public async Task<string> AcquireUserTokenAsync(string username, string password, CancellationToken cancellationToken)
    {
        var lastError = "(none)";
        for (var attempt = 1; ; attempt++)
        {
            // The user is provisioned asynchronously and inherits the realm's default required actions,
            // which block the direct-grant flow (production login uses the browser flow); clear them once
            // the user exists, then the grant succeeds.
            await ClearRequiredActionsAsync(username, cancellationToken);
            var (token, error) = await RequestUserTokenAsync(username, password, cancellationToken);
            if (token is not null)
            {
                return token;
            }

            lastError = error;
            if (attempt >= 30)
            {
                throw new InvalidOperationException(
                    $"Could not acquire a token for '{username}' after retries. Last response: {lastError}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    public async Task<bool> CanAuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        await ClearRequiredActionsAsync(username, cancellationToken);
        return (await RequestUserTokenAsync(username, password, cancellationToken)).Token is not null;
    }

    // Polls organization's own database for the employee's provisioning state until it reaches a terminal
    // state (Active/Failed) or the timeout — the saga converges asynchronously (FR-004). On timeout it
    // reports whether identity provisioned the Keycloak account, to localize a stalled round-trip.
    public async Task<Employee> WaitForTerminalStateAsync(Guid employeeId, string email, CancellationToken cancellationToken)
    {
        var deadlineAt = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);
        while (DateTimeOffset.UtcNow < deadlineAt)
        {
            await using var context = CreateOrganizationContext();
            var employee = await new EmployeeRepository(context)
                .GetByIdentifierAsync(EmployeeIdentifier.From(employeeId), cancellationToken);

            if (employee.IsSuccess && employee.Value.State != ProvisioningState.Provisioning)
            {
                return employee.Value;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        var keycloakUserExists = await KeycloakUserExistsAsync(email, cancellationToken);
        throw new InvalidOperationException(
            $"Employee {employeeId} stayed Provisioning. Keycloak account for '{email}' exists: {keycloakUserExists} "
            + "(true => identity consumed EmployeeHired and provisioned, so the ack did not reach organization; "
            + "false => EmployeeHired did not reach identity).");
    }

    public async Task<bool> KeycloakUserExistsAsync(string email, CancellationToken cancellationToken)
    {
        var adminToken = await AcquireMasterAdminTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"admin/realms/{Realm}/users?exact=true&email={Uri.EscapeDataString(email)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var response = await Keycloak.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var users = await response.Content.ReadFromJsonAsync<JsonArray>(cancellationToken) ?? [];
        return users.Count > 0;
    }

    private async Task<(string? Token, string Error)> RequestUserTokenAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"realms/{Realm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = BffClientId,
                ["client_secret"] = BffClientSecret,
                ["username"] = username,
                ["password"] = password,
            }),
        };

        using var response = await Keycloak.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return (null, $"{(int)response.StatusCode} {body}");
        }

        var payload = JsonNode.Parse(body);
        return (payload?["access_token"]?.GetValue<string>(), body);
    }

    // Completes a provisioned user so the test's direct-grant login is not blocked by "Account is not
    // fully set up": clears required actions, marks the email verified, and supplies a lastName (Keycloak
    // 26's declarative user profile requires it, and the provider only sets firstName). Best-effort; a
    // no-op if the user does not exist yet. This compensates for the browser-flow profile completion that
    // production login would otherwise handle.
    private async Task ClearRequiredActionsAsync(string username, CancellationToken cancellationToken)
    {
        var adminToken = await AcquireMasterAdminTokenAsync(cancellationToken);

        using var lookup = new HttpRequestMessage(
            HttpMethod.Get,
            $"admin/realms/{Realm}/users?exact=true&username={Uri.EscapeDataString(username)}");
        lookup.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var lookupResponse = await Keycloak.SendAsync(lookup, cancellationToken);
        if (!lookupResponse.IsSuccessStatusCode)
        {
            return;
        }

        var users = await lookupResponse.Content.ReadFromJsonAsync<JsonArray>(cancellationToken) ?? [];
        if (users.FirstOrDefault() is not JsonObject brief)
        {
            return;
        }

        var userId = brief["id"]!.GetValue<string>();

        using var fetch = new HttpRequestMessage(HttpMethod.Get, $"admin/realms/{Realm}/users/{userId}");
        fetch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var fetchResponse = await Keycloak.SendAsync(fetch, cancellationToken);
        if (await fetchResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken) is not { } user)
        {
            return;
        }

        user["requiredActions"] = new JsonArray();
        user["emailVerified"] = true;
        if (user["lastName"] is null)
        {
            user["lastName"] = "Test";
        }

        using var update = new HttpRequestMessage(HttpMethod.Put, $"admin/realms/{Realm}/users/{userId}")
        {
            Content = JsonContent.Create(user),
        };
        update.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var _ = await Keycloak.SendAsync(update, cancellationToken);
    }

    private async Task EnableDirectAccessGrantsAsync(CancellationToken cancellationToken)
    {
        var adminToken = await AcquireMasterAdminTokenAsync(cancellationToken);

        using var lookup = new HttpRequestMessage(HttpMethod.Get, $"admin/realms/{Realm}/clients?clientId={BffClientId}");
        lookup.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var lookupResponse = await Keycloak.SendAsync(lookup, cancellationToken);
        lookupResponse.EnsureSuccessStatusCode();

        var clients = await lookupResponse.Content.ReadFromJsonAsync<JsonArray>(cancellationToken) ?? [];
        var client = clients.FirstOrDefault()
            ?? throw new InvalidOperationException("The roomy-bff client was not found in the realm.");
        var clientId = client["id"]!.GetValue<string>();
        client["directAccessGrantsEnabled"] = true;

        using var update = new HttpRequestMessage(HttpMethod.Put, $"admin/realms/{Realm}/clients/{clientId}")
        {
            Content = JsonContent.Create(client),
        };
        update.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var updateResponse = await Keycloak.SendAsync(update, cancellationToken);
        updateResponse.EnsureSuccessStatusCode();
    }

    private async Task<string> AcquireMasterAdminTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "realms/master/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = MasterAdminUsername,
                ["password"] = MasterAdminPassword,
            }),
        };

        using var response = await Keycloak.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
        return payload?["access_token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Keycloak returned no admin token.");
    }

    private OrganizationDbContext CreateOrganizationContext() =>
        new(new DbContextOptionsBuilder<OrganizationDbContext>().UseNpgsql(organizationConnectionString).Options);

    private Uri Endpoint(string resource)
    {
        var endpoint = application!.GetEndpoint(resource, "http");
        var url = endpoint.AbsoluteUri.EndsWith('/') ? endpoint.AbsoluteUri : endpoint.AbsoluteUri + "/";
        return new Uri(url);
    }

    private HttpClient Keycloak => keycloak ?? throw new InvalidOperationException("The fixture is not initialised.");

    public async ValueTask DisposeAsync()
    {
        Organization.Dispose();
        keycloak?.Dispose();
        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}
