using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.Users;

public sealed class DisplayNameTests
{
    [Fact]
    public void From_trims_surrounding_whitespace()
    {
        DisplayName.From("  Ada Lovelace  ").Value.ShouldBe("Ada Lovelace");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_rejects_blank_input(string candidate)
    {
        Should.Throw<ArgumentException>(() => DisplayName.From(candidate));
    }

    [Fact]
    public void TryFrom_returns_the_trimmed_name()
    {
        DisplayName.TryFrom("  Ada  ").ShouldBe(DisplayName.From("Ada"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryFrom_returns_null_for_blank_input(string candidate)
    {
        DisplayName.TryFrom(candidate).ShouldBeNull();
    }

    [Fact]
    public void Equality_is_by_value()
    {
        DisplayName.From("Ada").ShouldBe(DisplayName.From("Ada"));
    }
}
