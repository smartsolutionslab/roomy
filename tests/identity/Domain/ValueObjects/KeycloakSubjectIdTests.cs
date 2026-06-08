using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.ValueObjects;

public sealed class KeycloakSubjectIdTests
{
    [Fact]
    public void From_wraps_an_existing_subject()
    {
        var value = Guid.NewGuid();

        KeycloakSubjectId.From(value).Value.ShouldBe(value);
    }

    [Fact]
    public void From_rejects_the_empty_guid()
    {
        Should.Throw<ArgumentException>(() => KeycloakSubjectId.From(Guid.Empty));
    }

    [Fact]
    public void Equality_is_by_value()
    {
        var value = Guid.NewGuid();

        KeycloakSubjectId.From(value).ShouldBe(KeycloakSubjectId.From(value));
    }
}
