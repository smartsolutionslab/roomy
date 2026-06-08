using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// The identity of a User account: a branded, time-ordered GUIDv7 so it can never be confused with
// another identifier (no primitive obsession). Minted with New() on registration, or From()/TryFrom()
// when rehydrating. The implicit Guid conversions keep the EF Core value converter trivial.
public readonly record struct UserIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static UserIdentifier New() => new() { Value = Guid.CreateVersion7() };

    public static UserIdentifier From(Guid value) =>
        TryFrom(value) ?? throw new ArgumentException("UserIdentifier must not be empty.", nameof(value));

    public static UserIdentifier? TryFrom(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(UserIdentifier identifier) => identifier.Value;

    public static implicit operator UserIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
