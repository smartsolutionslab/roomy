using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.ValueObjects;

public sealed class CapacityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(250)]
    public void From_accepts_a_positive_whole_number(int value)
    {
        Capacity.From(value).Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void From_rejects_a_value_below_one(int value)
    {
        Should.Throw<ArgumentException>(() => Capacity.From(value));
    }

    [Fact]
    public void TryParse_returns_null_below_one()
    {
        Capacity.TryParse(0).ShouldBeNull();
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Capacity.From(8).ShouldBe(Capacity.From(8));
    }
}
