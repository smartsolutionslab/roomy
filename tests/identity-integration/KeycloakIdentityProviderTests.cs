using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class KeycloakIdentityProviderTests(KeycloakRealmFixture fixture)
    : IClassFixture<KeycloakRealmFixture>
{
    private const string ValidPassword = "correct horse battery";

    private static Email UniqueEmail() => Email.From($"user-{Guid.NewGuid():N}@example.com");

    [Fact]
    public async Task Provisions_an_employee_and_assigns_the_employee_role()
    {
        var provider = fixture.CreateProvider();

        var result = await provider.ProvisionUserAsync(
            UniqueEmail(),
            DisplayName.From("Ada Lovelace"),
            ValidPassword,
            Role.Employee,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldNotBe(Guid.Empty);

        var roles = await fixture.GetRealmRoleNamesAsync(result.Value, TestContext.Current.CancellationToken);
        roles.ShouldContain("employee");
        roles.ShouldNotContain("administrator");
    }

    [Fact]
    public async Task Provisions_an_administrator_and_assigns_both_roles()
    {
        var provider = fixture.CreateProvider();

        var result = await provider.ProvisionUserAsync(
            UniqueEmail(),
            DisplayName.From("Grace Hopper"),
            ValidPassword,
            Role.Employee.GrantAdministrator(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        var roles = await fixture.GetRealmRoleNamesAsync(result.Value, TestContext.Current.CancellationToken);
        roles.ShouldContain("employee");
        roles.ShouldContain("administrator");
    }

    [Fact]
    public async Task Rejects_a_duplicate_email_as_email_taken()
    {
        var provider = fixture.CreateProvider();
        var email = UniqueEmail();

        var first = await provider.ProvisionUserAsync(
            email, DisplayName.From("First"), ValidPassword, Role.Employee, TestContext.Current.CancellationToken);
        first.IsSuccess.ShouldBeTrue();

        var second = await provider.ProvisionUserAsync(
            email, DisplayName.From("Second"), ValidPassword, Role.Employee, TestContext.Current.CancellationToken);

        second.IsFailure.ShouldBeTrue();
        second.Error.Type.ShouldBe(ErrorType.Conflict);
        second.Error.Code.ShouldBe("email_taken");
    }

    [Fact]
    public async Task Assigns_the_administrator_role_to_a_provisioned_employee()
    {
        var provider = fixture.CreateProvider();
        var provisioned = await provider.ProvisionUserAsync(
            UniqueEmail(), DisplayName.From("Ada Lovelace"), ValidPassword, Role.Employee,
            TestContext.Current.CancellationToken);
        provisioned.IsSuccess.ShouldBeTrue();

        var result = await provider.AssignAdministratorRoleAsync(
            provisioned.Value, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var roles = await fixture.GetRealmRoleNamesAsync(
            provisioned.Value, TestContext.Current.CancellationToken);
        roles.ShouldContain("administrator");
        roles.ShouldContain("employee");
    }

    [Fact]
    public async Task Assigning_the_administrator_role_is_idempotent()
    {
        var provider = fixture.CreateProvider();
        var provisioned = await provider.ProvisionUserAsync(
            UniqueEmail(), DisplayName.From("Grace Hopper"), ValidPassword,
            Role.Employee.GrantAdministrator(), TestContext.Current.CancellationToken);
        provisioned.IsSuccess.ShouldBeTrue();

        var result = await provider.AssignAdministratorRoleAsync(
            provisioned.Value, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var roles = await fixture.GetRealmRoleNamesAsync(
            provisioned.Value, TestContext.Current.CancellationToken);
        roles.Count(role => role == "administrator").ShouldBe(1);
    }

    [Fact]
    public async Task Rejects_a_password_below_the_minimum_length()
    {
        var provider = fixture.CreateProvider();

        var result = await provider.ProvisionUserAsync(
            UniqueEmail(),
            DisplayName.From("Short Pass"),
            "short",
            Role.Employee,
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("password_rejected");
    }
}
