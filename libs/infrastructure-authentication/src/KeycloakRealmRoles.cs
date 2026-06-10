using System.Security.Claims;
using System.Text.Json;

namespace SmartSolutionsLab.Roomy.Infrastructure.Authentication;

// Keycloak carries realm roles in a nested `realm_access.roles` array, which the JWT handler surfaces as a
// single JSON-valued claim rather than individual role claims. The admin authorization policy gates on
// RequireRole, so the host flattens those roles to ClaimTypes.Role claims on the BFF-forwarded token
// (ADR-0013). Login and session remain the BFF's concern; this only shapes claims for authorization.
public static class KeycloakRealmRoles
{
    private const string RealmAccessClaim = "realm_access";

    public static void AddRoleClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = identity.FindFirst(RealmAccessClaim)?.Value;
        if (string.IsNullOrEmpty(realmAccess))
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var roles)
            || roles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var name = role.GetString();
            if (!string.IsNullOrEmpty(name) && !identity.HasClaim(ClaimTypes.Role, name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, name));
            }
        }
    }
}
