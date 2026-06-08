using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.ValueObjects;

public sealed class DisplayNameTests
{
    [Fact]
    public void Create_trims_surrounding_whitespace()
    {
        DisplayName.Create("  Ada Lovelace  ").Value.ShouldBe("Ada Lovelace");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_input(string candidate)
    {
        Should.Throw<ArgumentException>(() => DisplayName.Create(candidate));
    }

    [Fact]
    public void Equality_is_by_value()
    {
        DisplayName.Create("Ada").ShouldBe(DisplayName.Create("Ada"));
    }
}
