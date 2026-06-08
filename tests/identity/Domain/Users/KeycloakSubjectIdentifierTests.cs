using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Domain.Users;

public sealed class KeycloakSubjectIdentifierTests
{
    [Fact]
    public void From_wraps_an_existing_subject()
    {
        var value = Guid.NewGuid();

        KeycloakSubjectIdentifier.From(value).Value.ShouldBe(value);
    }

    [Fact]
    public void From_rejects_the_empty_guid()
    {
        Should.Throw<ArgumentException>(() => KeycloakSubjectIdentifier.From(Guid.Empty));
    }

    [Fact]
    public void TryParse_returns_null_for_the_empty_guid()
    {
        KeycloakSubjectIdentifier.TryParse(Guid.Empty).ShouldBeNull();
    }

    [Fact]
    public void TryParse_returns_the_identifier_for_a_valid_value()
    {
        var value = Guid.NewGuid();

        KeycloakSubjectIdentifier.TryParse(value).ShouldBe(KeycloakSubjectIdentifier.From(value));
    }

    [Fact]
    public void Converts_implicitly_to_and_from_guid()
    {
        var value = Guid.NewGuid();

        KeycloakSubjectIdentifier identifier = value;
        Guid roundTripped = identifier;

        roundTripped.ShouldBe(value);
    }

    [Fact]
    public void Equality_is_by_value()
    {
        var value = Guid.NewGuid();

        KeycloakSubjectIdentifier.From(value).ShouldBe(KeycloakSubjectIdentifier.From(value));
    }
}
