using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// The user's human-readable name: required and trimmed. Equality is by value.
public sealed record DisplayName : IValueObject
{
    public string Value { get; }

    private DisplayName(string value) => Value = value;

    public static DisplayName From(string value) =>
        TryFrom(value) ?? throw new ArgumentException("DisplayName must not be blank.", nameof(value));

    public static DisplayName? TryFrom(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new DisplayName(value.Trim());
    }

    public override string ToString() => Value;
}
