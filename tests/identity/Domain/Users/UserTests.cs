using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.Users;

public sealed class UserTests
{
    private static User RegisterEmployee() =>
        User.Register(Email.From("ada@example.com"), DisplayName.From("Ada Lovelace"), Role.Employee);

    [Fact]
    public void Register_starts_a_provisioning_account_with_no_keycloak_link()
    {
        var user = RegisterEmployee();

        user.Status.ShouldBe(UserStatus.Provisioning);
        user.KeycloakSubjectIdentifier.ShouldBeNull();
        user.Identifier.Value.ShouldNotBe(Guid.Empty);
        user.Email.Value.ShouldBe("ada@example.com");
        user.DisplayName.Value.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public void Register_as_employee_is_an_employee_and_not_an_administrator()
    {
        var user = RegisterEmployee();

        user.IsEmployee.ShouldBeTrue();
        user.IsAdministrator.ShouldBeFalse();
    }

    [Fact]
    public void Register_as_administrator_is_an_administrator_and_still_an_employee()
    {
        var user = User.Register(
            Email.From("grace@example.com"),
            DisplayName.From("Grace Hopper"),
            Role.Employee.GrantAdministrator());

        user.IsAdministrator.ShouldBeTrue();
        user.IsEmployee.ShouldBeTrue();
    }

    [Fact]
    public void Activate_completes_provisioning_and_links_the_keycloak_subject()
    {
        var user = RegisterEmployee();
        var subject = KeycloakSubjectIdentifier.From(Guid.NewGuid());

        user.Activate(subject);

        user.Status.ShouldBe(UserStatus.Active);
        user.KeycloakSubjectIdentifier.ShouldBe(subject);
    }

    [Fact]
    public void Activate_rejects_an_account_that_is_not_provisioning()
    {
        var user = RegisterEmployee();
        user.Activate(KeycloakSubjectIdentifier.From(Guid.NewGuid()));

        Should.Throw<InvalidOperationException>(
            () => user.Activate(KeycloakSubjectIdentifier.From(Guid.NewGuid())));
    }
}
