using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

public readonly record struct KeycloakSubjectIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static KeycloakSubjectIdentifier From(Guid value) =>
        TryParse(value)
        ?? throw new ArgumentException("KeycloakSubjectIdentifier must not be empty.", nameof(value));

    public static KeycloakSubjectIdentifier? TryParse(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(KeycloakSubjectIdentifier identifier) => identifier.Value;

    public static implicit operator KeycloakSubjectIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
