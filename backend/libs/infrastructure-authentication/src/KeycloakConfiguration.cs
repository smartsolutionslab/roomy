using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Authentication;

public static class KeycloakConfiguration
{
    // Reads the Keycloak coordinates every host needs (ADR-0013/0045): the realm base address (required)
    // and the realm name (default "roomy"). Admin credentials stay with the one host that provisions.
    public static (Uri BaseAddress, string Realm) ReadKeycloak(this IConfiguration configuration)
    {
        var keycloak = configuration.GetSection("Keycloak");
        var baseAddress = new Uri(keycloak["BaseAddress"]
            ?? throw new InvalidOperationException("Missing configuration 'Keycloak:BaseAddress'."));
        return (baseAddress, keycloak["Realm"] ?? "roomy");
    }
}
