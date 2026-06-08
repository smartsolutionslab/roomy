using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.ValueObjects;

public sealed class RoleTests
{
    [Fact]
    public void Employee_is_the_base_role_and_not_an_administrator()
    {
        Role.Employee.IsAdministrator.ShouldBeFalse();
    }

    [Fact]
    public void Granting_administrator_elevates_the_role()
    {
        Role.Employee.GrantAdministrator().IsAdministrator.ShouldBeTrue();
    }

    [Fact]
    public void Granting_administrator_is_idempotent()
    {
        var elevated = Role.Employee.GrantAdministrator();

        elevated.GrantAdministrator().ShouldBe(elevated);
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Role.Employee.ShouldBe(Role.Employee);
        Role.Employee.GrantAdministrator().ShouldNotBe(Role.Employee);
    }
}
