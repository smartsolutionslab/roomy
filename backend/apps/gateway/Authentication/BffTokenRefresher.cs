using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

public static class BffTokenRefresher
{
    public const string HttpClientName = "keycloak-token";

    private static readonly TimeSpan refreshSkew = TimeSpan.FromSeconds(30);

    public static async Task ValidateOrRefreshAsync(CookieValidatePrincipalContext context)
    {
        var expiresAtValue = context.Properties.GetTokenValue(OidcTokenNames.ExpiresAt);
        if (expiresAtValue is null) return;


        if (!DateTimeOffset.TryParse(expiresAtValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt))
        {
            return;
        }

        if (expiresAt - refreshSkew > DateTimeOffset.UtcNow) return;

        var refreshToken = context.Properties.GetTokenValue(OidcTokenNames.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        var services = context.HttpContext.RequestServices;
        var keycloak = services.GetRequiredService<IOptions<KeycloakOidcOptions>>().Value;
        var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);

        using HttpRequestMessage request = new(HttpMethod.Post, keycloak.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = OidcTokenNames.RefreshToken,
                ["client_id"] = keycloak.ClientId,
                ["client_secret"] = keycloak.ClientSecret,
                [OidcTokenNames.RefreshToken] = refreshToken,
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

        context.Properties.UpdateTokenValue(OidcTokenNames.AccessToken, refreshed.AccessToken);
        if (!string.IsNullOrEmpty(refreshed.RefreshToken))
        {
            context.Properties.UpdateTokenValue(OidcTokenNames.RefreshToken, refreshed.RefreshToken);
        }

        if (!string.IsNullOrEmpty(refreshed.IdToken))
        {
            context.Properties.UpdateTokenValue(OidcTokenNames.IdToken, refreshed.IdToken);
        }

        var renewedExpiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshed.ExpiresIn);
        context.Properties.UpdateTokenValue(OidcTokenNames.ExpiresAt, renewedExpiresAt.ToString("o", CultureInfo.InvariantCulture));

        context.ShouldRenew = true;
    }

    private sealed record TokenResponse
    {
        [JsonPropertyName(OidcTokenNames.AccessToken)]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName(OidcTokenNames.RefreshToken)]
        public string? RefreshToken { get; init; }

        [JsonPropertyName(OidcTokenNames.IdToken)]
        public string? IdToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
