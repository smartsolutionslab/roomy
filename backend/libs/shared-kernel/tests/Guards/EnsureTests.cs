using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Guards;

public class EnsureTests
{
    [Fact]
    public void IsNotEmpty_passes_for_a_non_empty_string()
    {
        var result = Ensure.That("Munich").IsNotEmpty();

        result.Value.ShouldBe("Munich");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsNotEmpty_throws_for_empty_or_null(string? value)
    {
        Should.Throw<ArgumentException>(() => Ensure.That(value).IsNotEmpty());
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_throws_for_whitespace()
    {
        Should.Throw<ArgumentException>(() => Ensure.That("   ").IsNotNullOrWhiteSpace());
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_returns_the_non_null_value()
    {
        string? maybeNull = "desk-42";

        var result = Ensure.That(maybeNull).IsNotNullOrWhiteSpace();

        result.Value.ShouldBe("desk-42");
    }

    [Fact]
    public void IsNotNull_returns_the_non_null_reference()
    {
        string? maybeNull = "ada";

        var result = Ensure.That(maybeNull).IsNotNull();

        result.Value.ShouldBe("ada");
    }

    [Fact]
    public void IsNotNull_throws_for_a_null_reference()
    {
        string? maybeNull = null;

        Should.Throw<ArgumentNullException>(() => Ensure.That(maybeNull).IsNotNull());
    }

    [Fact]
    public void Guard_captures_the_argument_name()
    {
        var customerName = string.Empty;

        var exception = Should.Throw<ArgumentException>(
            () => Ensure.That(customerName).IsNotEmpty());

        exception.ParamName.ShouldBe(nameof(customerName));
    }

    private enum Sample
    {
        First,
        Second,
    }

    [Fact]
    public void IsEnum_parses_a_known_name_case_insensitively()
    {
        Ensure.That("second").IsEnum<Sample>().Value.ShouldBe(Sample.Second);
    }

    [Theory]
    [InlineData("Third")]
    [InlineData("5")]
    [InlineData("")]
    [InlineData(null)]
    public void IsEnum_throws_for_a_value_that_is_not_a_defined_member(string? value)
    {
        Should.Throw<ArgumentException>(() => Ensure.That(value).IsEnum<Sample>());
    }

    [Fact]
    public void IsPositive_throws_for_zero_or_negative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Ensure.That(0).IsPositive());
    }

    [Fact]
    public void Satisfies_throws_when_the_predicate_fails()
    {
        Should.Throw<ArgumentException>(
            () => Ensure.That(3).Satisfies(value => value > 10, "Value must exceed 10."));
    }

    [Fact]
    public void Guards_return_the_value()
    {
        var result = Ensure.That("desk-42").IsNotNullOrWhiteSpace();

        result.Value.ShouldBe("desk-42");
    }
}
