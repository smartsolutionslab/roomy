namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;

// Configuration for the Keycloak admin adapter. The HTTP client carries the Keycloak base address;
// these settle which realm the accounts live in and how the adapter authenticates to the Admin REST
// API. The admin credentials are supplied by configuration/secrets at the composition root, never
// hard-coded. Defaults match the dev realm import (research R1/R2/R5).
public sealed class KeycloakAdminOptions
{
    // The realm accounts are provisioned into.
    public string Realm { get; init; } = "roomy";

    // The realm whose token endpoint issues the admin access token (Keycloak's own master realm).
    public string AdminRealm { get; init; } = "master";

    // The client used for the admin password grant. Keycloak ships `admin-cli` for exactly this.
    public string AdminClientId { get; init; } = "admin-cli";

    public required string AdminUsername { get; init; }

    public required string AdminPassword { get; init; }
}
