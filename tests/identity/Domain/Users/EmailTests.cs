using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.Users;

public sealed class EmailTests
{
    [Fact]
    public void From_trims_and_lower_cases_the_address()
    {
        Email.From("  Ada.Lovelace@Example.COM ").Value.ShouldBe("ada.lovelace@example.com");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@example.com")]
    [InlineData("spaces in@example.com")]
    public void From_rejects_syntactically_invalid_addresses(string candidate)
    {
        Should.Throw<ArgumentException>(() => Email.From(candidate));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_rejects_blank_input(string candidate)
    {
        Should.Throw<ArgumentException>(() => Email.From(candidate));
    }

    [Fact]
    public void TryParse_returns_the_normalized_email_for_a_valid_address()
    {
        Email.TryParse("  Ada@Example.com ").ShouldBe(Email.From("ada@example.com"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void TryParse_returns_null_for_invalid_input(string candidate)
    {
        Email.TryParse(candidate).ShouldBeNull();
    }

    [Fact]
    public void Equality_is_by_normalized_value()
    {
        Email.From("Ada@Example.com").ShouldBe(Email.From("ada@example.com"));
    }
}
