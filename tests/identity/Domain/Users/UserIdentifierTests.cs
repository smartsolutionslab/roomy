using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.Users;

public sealed class UserIdentifierTests
{
    [Fact]
    public void New_generates_a_non_empty_identifier()
    {
        UserIdentifier.New().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void New_generates_distinct_identifiers()
    {
        UserIdentifier.New().ShouldNotBe(UserIdentifier.New());
    }

    [Fact]
    public void From_wraps_an_existing_value()
    {
        var value = Guid.NewGuid();

        UserIdentifier.From(value).Value.ShouldBe(value);
    }

    [Fact]
    public void From_rejects_the_empty_guid()
    {
        Should.Throw<ArgumentException>(() => UserIdentifier.From(Guid.Empty));
    }

    [Fact]
    public void TryParse_returns_null_for_the_empty_guid()
    {
        UserIdentifier.TryParse(Guid.Empty).ShouldBeNull();
    }

    [Fact]
    public void TryParse_returns_the_identifier_for_a_valid_value()
    {
        var value = Guid.NewGuid();

        UserIdentifier.TryParse(value).ShouldBe(UserIdentifier.From(value));
    }

    [Fact]
    public void Converts_implicitly_to_and_from_guid()
    {
        var value = Guid.CreateVersion7();

        UserIdentifier identifier = value;
        Guid roundTripped = identifier;

        roundTripped.ShouldBe(value);
    }

    [Fact]
    public void Equality_is_by_value()
    {
        var value = Guid.NewGuid();

        UserIdentifier.From(value).ShouldBe(UserIdentifier.From(value));
    }
}
