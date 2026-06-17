using System.ComponentModel.DataAnnotations;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

public sealed class KeycloakOidcOptions
{
    public const string SectionName = "Authentication:Keycloak";

    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string Realm { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string RealmUrl => $"{Authority.TrimEnd('/')}/realms/{Realm}";

    public string TokenEndpoint => $"{RealmUrl}/protocol/openid-connect/token";
}
