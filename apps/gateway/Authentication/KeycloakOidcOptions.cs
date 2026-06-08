using System.ComponentModel.DataAnnotations;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

// Binds the Keycloak OIDC settings the gateway needs as the confidential client (ADR-0013).
// In local development these come from the Aspire-injected Keycloak connection plus the
// `Authentication:Keycloak` configuration section; in other environments from config/secrets.
public sealed class KeycloakOidcOptions
{
    public const string SectionName = "Authentication:Keycloak";

    // Base URL of the Keycloak server (e.g. https://localhost:8443). Combined with the realm
    // to form the OIDC authority. Supplied by Aspire service discovery in local dev.
    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string Realm { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    // HTTPS metadata is required everywhere except local development, where Keycloak may be
    // reached over plain HTTP through the Aspire network.
    public bool RequireHttpsMetadata { get; set; } = true;
}
