using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.Employees;

public sealed class EmployeeValueObjectTests
{
    [Fact]
    public void EmployeeIdentifier_round_trips_through_guid()
    {
        var id = EmployeeIdentifier.New();
        ((EmployeeIdentifier)id.Value).ShouldBe(id);
    }

    [Fact]
    public void Identifiers_reject_empty()
    {
        Should.Throw<ArgumentException>(() => EmployeeIdentifier.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => UserIdentifier.From(Guid.Empty));
    }

    [Fact]
    public void EmployeeName_trims_and_keeps_the_value()
    {
        EmployeeName.From("  Ada Lovelace  ").Value.ShouldBe("Ada Lovelace");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmployeeName_rejects_blank(string value)
    {
        Should.Throw<ArgumentException>(() => EmployeeName.From(value));
    }

    [Fact]
    public void WorkEmail_normalizes_to_trimmed_lowercase()
    {
        WorkEmail.From("  Ada@Example.COM ").Value.ShouldBe("ada@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("ada@")]
    [InlineData("ada@example")]
    [InlineData("ada@@example.com")]
    public void WorkEmail_rejects_malformed(string value)
    {
        Should.Throw<ArgumentException>(() => WorkEmail.From(value));
    }

    [Fact]
    public void WorkEmail_equality_is_by_value()
    {
        WorkEmail.From("ada@example.com").ShouldBe(WorkEmail.From("ADA@example.com"));
    }
}
