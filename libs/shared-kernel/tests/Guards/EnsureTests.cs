using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Guards;

public class EnsureTests
{
    [Fact]
    public void IsNotEmpty_passes_for_a_non_empty_string()
    {
        var result = Ensure.That("Munich").IsNotEmpty();

        Assert.Equal("Munich", result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsNotEmpty_throws_for_empty_or_null(string? value)
    {
        Assert.Throws<ArgumentException>(() => Ensure.That(value!).IsNotEmpty());
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_throws_for_whitespace()
    {
        Assert.Throws<ArgumentException>(() => Ensure.That("   ").IsNotNullOrWhiteSpace());
    }

    [Fact]
    public void Guard_captures_the_argument_name()
    {
        var customerName = string.Empty;

        var exception = Assert.Throws<ArgumentException>(
            () => Ensure.That(customerName).IsNotEmpty());

        Assert.Equal(nameof(customerName), exception.ParamName);
    }

    [Fact]
    public void IsPositive_throws_for_zero_or_negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Ensure.That(0).IsPositive());
    }

    [Fact]
    public void Satisfies_throws_when_the_predicate_fails()
    {
        Assert.Throws<ArgumentException>(
            () => Ensure.That(3).Satisfies(value => value > 10, "Value must exceed 10."));
    }

    [Fact]
    public void Guards_chain_and_return_the_value()
    {
        var result = Ensure.That("desk-42").IsNotEmpty().IsNotNullOrWhiteSpace();

        Assert.Equal("desk-42", result.Value);
    }
}
