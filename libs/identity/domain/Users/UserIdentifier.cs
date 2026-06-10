using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

public readonly record struct UserIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static UserIdentifier New() => new() { Value = Guid.CreateVersion7() };

    public static UserIdentifier From(Guid value) =>
        TryParse(value) ?? throw new ArgumentException("UserIdentifier must not be empty.", nameof(value));

    public static UserIdentifier? TryParse(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(UserIdentifier identifier) => identifier.Value;

    public static implicit operator UserIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
