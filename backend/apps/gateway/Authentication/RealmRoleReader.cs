using System.Text.Json;

namespace SmartSolutionsLab.Roomy.Gateway.Authentication;

// Reads the realm roles out of Keycloak's realm_access claim value, which is a JSON object
// of the shape { "roles": ["employee", "administrator"] }. Pure and side-effect free so it
// can be unit-tested without a live identity provider.
public static class RealmRoleReader
{
    private const string RolesProperty = "roles";

    public static IReadOnlyList<string> ReadRoles(string? realmAccessJson)
    {
        if (string.IsNullOrWhiteSpace(realmAccessJson)) return [];

        using var document = TryParse(realmAccessJson);
        if (document is null) return [];

        if (!document.RootElement.TryGetProperty(RolesProperty, out var roles) || roles.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<string>(roles.GetArrayLength());
        foreach (var role in roles.EnumerateArray())
        {
            if (role.ValueKind != JsonValueKind.String) continue;

            var value = role.GetString();
            if (!string.IsNullOrWhiteSpace(value)) result.Add(value);
        }

        return result;
    }

    private static JsonDocument? TryParse(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            // A malformed realm_access claim yields no roles rather than failing the sign-in.
            return null;
        }
    }
}
