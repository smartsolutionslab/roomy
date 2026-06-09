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
    public void Register_with_an_identifier_uses_the_supplied_identifier()
    {
        // The provisioning saga pre-allocates the UserId as the correlation key for the 1:1
        // User<->Employee link (ADR-0025), so registration must honour it rather than mint a new one.
        var identifier = UserIdentifier.New();

        var user = User.Register(
            identifier,
            Email.From("alan@example.com"),
            DisplayName.From("Alan Turing"),
            Role.Employee);

        user.Identifier.ShouldBe(identifier);
        user.Status.ShouldBe(UserStatus.Provisioning);
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

    [Fact]
    public void GrantAdministrator_elevates_an_employee_and_keeps_the_employee_role()
    {
        var user = RegisterEmployee();

        user.GrantAdministrator(grantedAt);

        user.IsAdministrator.ShouldBeTrue();
        user.IsEmployee.ShouldBeTrue();
    }

    [Fact]
    public void GrantAdministrator_raises_an_administrator_granted_event()
    {
        var user = RegisterEmployee();

        user.GrantAdministrator(grantedAt);

        var raised = user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AdministratorGranted>();
        raised.UserId.ShouldBe(user.Identifier);
        raised.OccurredAt.ShouldBe(grantedAt);
    }

    [Fact]
    public void GrantAdministrator_is_idempotent_and_raises_no_further_event()
    {
        var user = RegisterEmployee();

        user.GrantAdministrator(grantedAt);
        user.GrantAdministrator(grantedAt.AddMinutes(1));

        user.IsAdministrator.ShouldBeTrue();
        user.DomainEvents.ShouldHaveSingleItem();
    }

    [Fact]
    public void GrantAdministrator_raises_nothing_for_an_account_already_an_administrator()
    {
        var user = User.Register(
            Email.From("grace@example.com"),
            DisplayName.From("Grace Hopper"),
            Role.Employee.GrantAdministrator());

        user.GrantAdministrator(grantedAt);

        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_empties_the_recorded_events()
    {
        var user = RegisterEmployee();
        user.GrantAdministrator(grantedAt);

        user.ClearDomainEvents();

        user.DomainEvents.ShouldBeEmpty();
    }

    private static readonly DateTimeOffset grantedAt = new(2026, 6, 9, 12, 0, 0, TimeSpan.Zero);
}
