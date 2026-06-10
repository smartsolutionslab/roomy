using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class KeycloakRealmFixture : IAsyncLifetime
{
    public const string Realm = "roomy";
    public const string AdminUsername = "admin";
    public const string AdminPassword = "admin-test-only-password";

    private DistributedApplication? application;
    private HttpClient? httpClient;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Roomy_Identity_KeycloakTestAppHost>();

        application = await builder.BuildAsync();
        await application.StartAsync();

        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var notifications = application.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync("keycloak", readiness.Token);

        var endpoint = application.GetEndpoint("keycloak", "http");
        var baseUrl = endpoint.AbsoluteUri.EndsWith('/') ? endpoint.AbsoluteUri : endpoint.AbsoluteUri + "/";
        httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public KeycloakIdentityProvider CreateProvider() =>
        new(
            httpClient ?? throw new InvalidOperationException("The fixture is not initialised."),
            new KeycloakAdminOptions
            {
                Realm = Realm,
                AdminUsername = AdminUsername,
                AdminPassword = AdminPassword,
            });

    public async Task<IReadOnlyList<string>> GetRealmRoleNamesAsync(
        KeycloakSubjectIdentifier subject,
        CancellationToken cancellationToken)
    {
        var token = await AcquireAdminTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"admin/realms/{Realm}/users/{subject.Value}/role-mappings/realm");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await Client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var roles = await response.Content.ReadFromJsonAsync<JsonArray>(cancellationToken)
            ?? new JsonArray();
        return roles
            .Select(role => role?["name"]?.GetValue<string>())
            .Where(name => name is not null)
            .Select(name => name!)
            .ToList();
    }

    private HttpClient Client =>
        httpClient ?? throw new InvalidOperationException("The fixture is not initialised.");

    private async Task<string> AcquireAdminTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "realms/master/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = AdminUsername,
                ["password"] = AdminPassword,
            }),
        };

        using var response = await Client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
        return payload?["access_token"]?.GetValue<string>() ?? throw new InvalidOperationException("Keycloak returned no access token.");
    }

    public async ValueTask DisposeAsync()
    {
        httpClient?.Dispose();
        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}
