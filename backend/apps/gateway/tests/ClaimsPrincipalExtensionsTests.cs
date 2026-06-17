using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;
using SmartSolutionsLab.Roomy.Gateway.Bff;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void Projects_preferred_username_and_roles()
    {
        var principal = PrincipalWith(
            new Claim(JwtRegisteredClaimNames.PreferredUsername, "ada.lovelace"),
            new Claim(ClaimTypes.Role, "employee"),
            new Claim(ClaimTypes.Role, "administrator"));

        var currentUser = principal.ToCurrentUser();

        currentUser.Name.ShouldBe("ada.lovelace");
        currentUser.Roles.ShouldBe(["employee", "administrator"]);
    }

    [Fact]
    public void Falls_back_to_name_claim_when_preferred_username_is_absent()
    {
        var principal = PrincipalWith(new Claim(JwtRegisteredClaimNames.Name, "Grace Hopper"));

        var currentUser = principal.ToCurrentUser();

        currentUser.Name.ShouldBe("Grace Hopper");
    }

    [Fact]
    public void Deduplicates_repeated_roles()
    {
        var principal = PrincipalWith(
            new Claim(ClaimTypes.Role, "employee"),
            new Claim(ClaimTypes.Role, "employee"));

        var currentUser = principal.ToCurrentUser();

        currentUser.Roles.ShouldBe(["employee"]);
    }

    [Fact]
    public void Has_no_token_material_on_the_projection()
    {
        var principal = PrincipalWith(
            new Claim(JwtRegisteredClaimNames.PreferredUsername, "ada"),
            new Claim("access_token", "a.b.c"),
            new Claim(ClaimTypes.Role, "employee"));

        var currentUser = principal.ToCurrentUser();

        currentUser.Name.ShouldBe("ada");
        currentUser.Roles.ShouldBe(["employee"]);
    }

    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(
            claims,
            authenticationType: "TestCookie",
            JwtRegisteredClaimNames.PreferredUsername, ClaimTypes.Role));
}
