using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;

public sealed class KeycloakIdentityProvider(HttpClient httpClient, KeycloakAdminOptions options)
    : IIdentityProviderPort
{
    public async Task<Result<KeycloakSubjectIdentifier>> ProvisionUserAsync(
        UserIdentifier userIdentifier,
        Email email,
        DisplayName displayName,
        string initialPassword,
        Role role,
        CancellationToken cancellationToken)
    {
        var token = await AcquireAdminTokenAsync(cancellationToken);

        var creation = await CreateUserAsync(token, userIdentifier, email, displayName, initialPassword, cancellationToken);
        if (creation.IsFailure) return creation.Error;

        var subject = creation.Value;

        var assignment = await AssignRealmRolesAsync(token, subject, role, cancellationToken);
        if (assignment.IsFailure)
        {
            await RemovePartialUserAsync(token, subject, cancellationToken);
            return assignment.Error;
        }

        return subject;
    }

    public async Task<Result> AssignAdministratorRoleAsync(KeycloakSubjectIdentifier subject, CancellationToken cancellationToken)
    {
        var token = await AcquireAdminTokenAsync(cancellationToken);

        var representation = await FetchRealmRoleAsync(token, "administrator", cancellationToken);
        if (representation.IsFailure) return representation.Error;

        var requestUrl = $"admin/realms/{options.Realm}/users/{subject.Value}/role-mappings/realm";
        using var request = AdminRequest(HttpMethod.Post, requestUrl, token, new JsonArray(representation.Value));
        using var response = await httpClient.SendAsync(request, cancellationToken);

        return response.IsSuccessStatusCode ? Result.Success() : ProviderError(response);
    }

    private async Task<string> AcquireAdminTokenAsync(CancellationToken cancellationToken)
    {
        var requestUrl = $"realms/{options.AdminRealm}/protocol/openid-connect/token";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = options.AdminClientId,
                ["username"] = options.AdminUsername,
                ["password"] = options.AdminPassword,
            }),
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
        return payload?["access_token"]?.GetValue<string>() ?? throw new InvalidOperationException("Keycloak returned no admin access token.");
    }

    private async Task<Result<KeycloakSubjectIdentifier>> CreateUserAsync(
        string token,
        UserIdentifier userIdentifier,
        Email email,
        DisplayName displayName,
        string initialPassword,
        CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            // The token's roomy_user_id claim is mapped from this attribute so the API resolves the caller
            // by the Roomy UserIdentifier. Keycloak mints its own sub and ignores a forced id on create,
            // so the identifier has to travel as an attribute (ADR-0058).
            ["attributes"] = new JsonObject { ["roomy_user_id"] = new JsonArray(userIdentifier.Value.ToString()) },
            ["username"] = email.Value,
            ["email"] = email.Value,
            ["firstName"] = displayName.Value,
            ["enabled"] = true,
            ["emailVerified"] = true,
            ["credentials"] = new JsonArray(
                new JsonObject
                {
                    ["type"] = "password",
                    ["value"] = initialPassword,
                    ["temporary"] = false,
                }),
        };

        using var request = AdminRequest(HttpMethod.Post, $"admin/realms/{options.Realm}/users", token, body);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var location = response.Headers.Location ?? throw new InvalidOperationException("Keycloak create-user returned no Location header.");
            var subjectId = Guid.Parse(location.Segments[^1].Trim('/'));
            return KeycloakSubjectIdentifier.From(subjectId);
        }

        if (response.StatusCode == HttpStatusCode.Conflict) return Error.Conflict("email_taken", "An account already exists for this email.");
        if (response.StatusCode == HttpStatusCode.BadRequest) return Error.Validation("password_rejected", "The initial password does not satisfy the policy.");

        return ProviderError(response);
    }

    private async Task<Result> AssignRealmRolesAsync(string token, KeycloakSubjectIdentifier subject, Role role, CancellationToken cancellationToken)
    {
        var roleNames = role.IsAdministrator
            ? new[] { "employee", "administrator" }
            : new[] { "employee" };

        var representations = new JsonArray();
        foreach (var roleName in roleNames)
        {
            var representation = await FetchRealmRoleAsync(token, roleName, cancellationToken);
            if (representation.IsFailure) return representation.Error;

            representations.Add(representation.Value);
        }

        var requestUrl = $"admin/realms/{options.Realm}/users/{subject.Value}/role-mappings/realm";
        using var request = AdminRequest(HttpMethod.Post, requestUrl, token, representations);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        return response.IsSuccessStatusCode ? Result.Success() : ProviderError(response);
    }

    private async Task<Result<JsonNode>> FetchRealmRoleAsync(string token, string roleName, CancellationToken cancellationToken)
    {
        var requestUrl = $"admin/realms/{options.Realm}/roles/{roleName}";
        using var request = AdminRequest(HttpMethod.Get, requestUrl, token, content: null);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode) return ProviderError(response);

        var role = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
        var identifier = role?["id"]?.GetValue<string>();
        var name = role?["name"]?.GetValue<string>();
        if (identifier is null || name is null) return new Error("provider_error", "Keycloak returned an incomplete role representation.");

        return new JsonObject { ["id"] = identifier, ["name"] = name };
    }

    private async Task RemovePartialUserAsync(string token, KeycloakSubjectIdentifier subject, CancellationToken cancellationToken)
    {
        var requestUrl = $"admin/realms/{options.Realm}/users/{subject.Value}";
        using var request = AdminRequest(HttpMethod.Delete, requestUrl, token, content: null);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        _ = response;
    }

    private static HttpRequestMessage AdminRequest(HttpMethod method, string requestUri, string token, JsonNode? content)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (content is not null)
        {
            request.Content = new StringContent(content.ToJsonString(), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static Error ProviderError(HttpResponseMessage response) =>
        new("provider_error", $"The identity provider returned status {(int)response.StatusCode}.");
}
