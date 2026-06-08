using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Create_trims_and_lower_cases_the_address()
    {
        Email.Create("  Ada.Lovelace@Example.COM ").Value.ShouldBe("ada.lovelace@example.com");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@example.com")]
    [InlineData("spaces in@example.com")]
    public void Create_rejects_syntactically_invalid_addresses(string candidate)
    {
        Should.Throw<ArgumentException>(() => Email.Create(candidate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_input(string candidate)
    {
        Should.Throw<ArgumentException>(() => Email.Create(candidate));
    }

    [Fact]
    public void Equality_is_by_normalized_value()
    {
        Email.Create("Ada@Example.com").ShouldBe(Email.Create("ada@example.com"));
    }
}
