namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

// The OIDC token names stored in the auth-cookie properties and returned by the token endpoint.
// One source of truth so a typo can't silently break refresh/forwarding (no compile error otherwise).
internal static class OidcTokenNames
{
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";
    public const string IdToken = "id_token";
    public const string ExpiresAt = "expires_at";
}
