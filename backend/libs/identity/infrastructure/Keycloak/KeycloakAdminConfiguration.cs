using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Keycloak;

public static class KeycloakAdminConfiguration
{
    // The realm base address plus the admin credentials the provisioning hosts (identity-api, dev-seeder)
    // need to call the Keycloak admin API. Admin credentials stay with these hosts only (ADR-0013).
    public static (Uri BaseAddress, KeycloakAdminOptions Admin) ReadKeycloakAdmin(this IConfiguration configuration)
    {
        var keycloak = configuration.GetSection("Keycloak");
        var baseAddress = new Uri(keycloak["BaseAddress"] ?? throw new InvalidOperationException("Missing configuration 'Keycloak:BaseAddress'."));

        var admin = new KeycloakAdminOptions
        {
            Realm = keycloak["Realm"] ?? "roomy",
            AdminUsername = keycloak["AdminUsername"] ?? throw new InvalidOperationException("Missing configuration 'Keycloak:AdminUsername'."),
            AdminPassword = keycloak["AdminPassword"] ?? throw new InvalidOperationException("Missing configuration 'Keycloak:AdminPassword'."),
        };

        return (baseAddress, admin);
    }
}
