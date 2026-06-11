using System.Text.Json;
using Shouldly;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

// Guards the contract the BFF auth depends on. The gateway builds the signed-in identity from the ID
// token + UserInfo and then flattens realm_access.roles (BffAuthenticationExtensions.FlattenRealmRoles).
// Keycloak's default "roles" scope emits realm_access into the access token ONLY, so the roomy-bff client
// must carry a realm-roles mapper that also writes it into the ID token and UserInfo — otherwise the SPA
// session reports no roles and administrators lose their admin views even though Keycloak granted the role.
public sealed class KeycloakRealmImportTests
{
    private static JsonElement RealmRolesMapperConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "keycloak", "roomy-realm.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        var bff = document.RootElement
            .GetProperty("clients")
            .EnumerateArray()
            .Single(client => client.GetProperty("clientId").GetString() == "roomy-bff");

        var mapper = bff.GetProperty("protocolMappers")
            .EnumerateArray()
            .Single(candidate =>
                candidate.GetProperty("protocolMapper").GetString() == "oidc-usermodel-realm-role-mapper");

        return mapper.GetProperty("config").Clone();
    }

    [Fact]
    public void Bff_client_emits_realm_roles_into_the_id_token_and_userinfo()
    {
        var config = RealmRolesMapperConfig();

        config.GetProperty("claim.name").GetString().ShouldBe("realm_access.roles");
        // The fix: realm_access must reach the ID token and UserInfo, not only the access token.
        config.GetProperty("id.token.claim").GetString().ShouldBe("true");
        config.GetProperty("userinfo.token.claim").GetString().ShouldBe("true");
    }
}
