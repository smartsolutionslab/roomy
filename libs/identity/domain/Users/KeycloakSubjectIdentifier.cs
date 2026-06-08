using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// Link to the Keycloak user (the OIDC subject): a branded GUID set once provisioning succeeds.
// The identity context owns account/role data while Keycloak owns credentials (research R1/R2).
// The implicit Guid conversions keep the EF Core value converter trivial.
public readonly record struct KeycloakSubjectIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static KeycloakSubjectIdentifier From(Guid value) =>
        TryFrom(value)
        ?? throw new ArgumentException("KeycloakSubjectIdentifier must not be empty.", nameof(value));

    public static KeycloakSubjectIdentifier? TryFrom(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(KeycloakSubjectIdentifier identifier) => identifier.Value;

    public static implicit operator KeycloakSubjectIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
