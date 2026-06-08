using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

// Link to the Keycloak user (the OIDC subject): a branded GUID set once provisioning succeeds.
// The identity context owns account/role data while Keycloak owns credentials (research R1/R2).
public readonly record struct KeycloakSubjectId
{
    public Guid Value { get; private init; }

    public static KeycloakSubjectId From(Guid value)
    {
        Ensure.That(value)
            .Satisfies(candidate => candidate != Guid.Empty, "KeycloakSubjectId must not be empty.");

        return new() { Value = value };
    }

    public override string ToString() => Value.ToString();
}
