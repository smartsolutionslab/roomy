namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;

public sealed class KeycloakAdminOptions
{
    public string Realm { get; init; } = "roomy";

    public string AdminRealm { get; init; } = "master";

    public string AdminClientId { get; init; } = "admin-cli";

    public required string AdminUsername { get; init; }

    public required string AdminPassword { get; init; }
}
