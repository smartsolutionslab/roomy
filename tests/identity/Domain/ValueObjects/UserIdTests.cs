using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.ValueObjects;

public sealed class UserIdTests
{
    [Fact]
    public void New_generates_a_non_empty_identifier()
    {
        UserId.New().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void New_generates_distinct_identifiers()
    {
        UserId.New().ShouldNotBe(UserId.New());
    }

    [Fact]
    public void From_wraps_an_existing_value()
    {
        var value = Guid.NewGuid();

        UserId.From(value).Value.ShouldBe(value);
    }

    [Fact]
    public void From_rejects_the_empty_guid()
    {
        Should.Throw<ArgumentException>(() => UserId.From(Guid.Empty));
    }

    [Fact]
    public void Equality_is_by_value()
    {
        var value = Guid.NewGuid();

        UserId.From(value).ShouldBe(UserId.From(value));
    }
}
