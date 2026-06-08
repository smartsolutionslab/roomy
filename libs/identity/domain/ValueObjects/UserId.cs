using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

// The identity of a User account: a branded GUID so it can never be confused with another id
// (no primitive obsession). Minted with New() on registration, or From() when rehydrating.
public readonly record struct UserId
{
    public Guid Value { get; private init; }

    public static UserId New() => new() { Value = Guid.NewGuid() };

    public static UserId From(Guid value)
    {
        Ensure.That(value).Satisfies(candidate => candidate != Guid.Empty, "UserId must not be empty.");

        return new() { Value = value };
    }

    public override string ToString() => Value.ToString();
}
