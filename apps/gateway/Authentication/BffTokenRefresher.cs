using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

// Keeps the BFF's stored access token usable across the (longer-lived, sliding) cookie session. The
// cookie outlives Keycloak's short access-token lifetime, so without this the proxy would forward an
// expired token and the context APIs reject it ("Bearer was not authenticated … IDX10223 … token is
// expired"). On every cookie validation, if the access token is at/near expiry we exchange the
// refresh token (offline_access) for a fresh set at Keycloak's token endpoint and re-issue the cookie,
// so the token the proxy forwards downstream is always live. A failed refresh rejects the principal,
// forcing a clean re-login (ADR-0013).
public static class BffTokenRefresher
{
    public const string HttpClientName = "keycloak-token";

    // Refresh a little before the actual expiry so an in-flight request never carries a just-expired token.
    private static readonly TimeSpan refreshSkew = TimeSpan.FromSeconds(30);

    public static async Task ValidateOrRefreshAsync(CookieValidatePrincipalContext context)
    {
        var expiresAtValue = context.Properties.GetTokenValue("expires_at");
        if (expiresAtValue is null)
        {
            return; // no stored tokens on this principal — nothing to refresh
        }

        if (!DateTimeOffset.TryParse(
                expiresAtValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt))
        {
            return;
        }

        if (expiresAt - refreshSkew > DateTimeOffset.UtcNow)
        {
            return; // still valid
        }

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        var services = context.HttpContext.RequestServices;
        var keycloak = services.GetRequiredService<IOptions<KeycloakOidcOptions>>().Value;
        var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);

        var tokenEndpoint = string.Create(
            CultureInfo.InvariantCulture,
            $"{keycloak.Authority.TrimEnd('/')}/realms/{keycloak.Realm}/protocol/openid-connect/token");

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = keycloak.ClientId,
                ["client_secret"] = keycloak.ClientSecret,
                ["refresh_token"] = refreshToken,
            }),
        };

        using var response = await httpClient.SendAsync(request, context.HttpContext.RequestAborted);

        if (!response.IsSuccessStatusCode)
        {
            context.RejectPrincipal();
            return;
        }

        var refreshed = await response.Content.ReadFromJsonAsync<TokenResponse>(context.HttpContext.RequestAborted);

        if (refreshed is null || string.IsNullOrEmpty(refreshed.AccessToken))
        {
            context.RejectPrincipal();
            return;
        }

        context.Properties.UpdateTokenValue("access_token", refreshed.AccessToken);
        if (!string.IsNullOrEmpty(refreshed.RefreshToken))
        {
            context.Properties.UpdateTokenValue("refresh_token", refreshed.RefreshToken);
        }

        if (!string.IsNullOrEmpty(refreshed.IdToken))
        {
            context.Properties.UpdateTokenValue("id_token", refreshed.IdToken);
        }

        var renewedExpiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshed.ExpiresIn);
        context.Properties.UpdateTokenValue("expires_at", renewedExpiresAt.ToString("o", CultureInfo.InvariantCulture));

        // Persist the refreshed tokens back into the session cookie.
        context.ShouldRenew = true;
    }

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
