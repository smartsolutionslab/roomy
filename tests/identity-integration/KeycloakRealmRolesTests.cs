using System.Security.Claims;
using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// Keycloak carries realm roles in a nested realm_access.roles array that the JWT handler surfaces as one
// JSON-valued claim; the host flattens it to ClaimTypes.Role so the admin policy's RequireRole sees the
// administrator role (ADR-0013). A pure claims transformation — no host or token needed to verify it.
public sealed class KeycloakRealmRolesTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void Flattens_realm_access_roles_into_role_claims()
    {
        var principal = PrincipalWith(
            new Claim("realm_access", """{"roles":["employee","administrator"]}"""));

        KeycloakRealmRoles.AddRoleClaims(principal);

        principal.IsInRole("administrator").ShouldBeTrue();
        principal.IsInRole("employee").ShouldBeTrue();
    }

    [Fact]
    public void Adds_nothing_when_there_is_no_realm_access_claim()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        KeycloakRealmRoles.AddRoleClaims(principal);

        principal.Claims.ShouldNotContain(claim => claim.Type == ClaimTypes.Role);
    }

    [Fact]
    public void Is_idempotent_and_does_not_duplicate_role_claims()
    {
        var principal = PrincipalWith(new Claim("realm_access", """{"roles":["administrator"]}"""));

        KeycloakRealmRoles.AddRoleClaims(principal);
        KeycloakRealmRoles.AddRoleClaims(principal);

        principal.Claims.Count(claim => claim is { Type: ClaimTypes.Role, Value: "administrator" })
            .ShouldBe(1);
    }
}
