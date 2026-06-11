using Shouldly;
using SmartSolutionsLab.Roomy.Gateway.Authentication;

namespace SmartSolutionsLab.Roomy.Gateway.Tests;

public sealed class RealmRoleReaderTests
{
    [Fact]
    public void Reads_realm_roles_from_keycloak_realm_access_claim()
    {
        const string RealmAccess = """{"roles":["employee","administrator"]}""";

        var roles = RealmRoleReader.ReadRoles(RealmAccess);

        roles.ShouldBe(["employee", "administrator"]);
    }

    [Fact]
    public void Returns_no_roles_when_the_claim_is_absent()
    {
        var roles = RealmRoleReader.ReadRoles(null);

        roles.ShouldBeEmpty();
    }

    [Fact]
    public void Returns_no_roles_when_the_claim_has_no_roles_array()
    {
        const string RealmAccess = """{"other":"value"}""";

        var roles = RealmRoleReader.ReadRoles(RealmAccess);

        roles.ShouldBeEmpty();
    }

    [Fact]
    public void Returns_no_roles_when_the_claim_is_malformed_json()
    {
        var roles = RealmRoleReader.ReadRoles("not-json");

        roles.ShouldBeEmpty();
    }

    [Fact]
    public void Skips_blank_and_non_string_role_entries()
    {
        const string RealmAccess = """{"roles":["employee","",42,"administrator"]}""";

        var roles = RealmRoleReader.ReadRoles(RealmAccess);

        roles.ShouldBe(["employee", "administrator"]);
    }
}
