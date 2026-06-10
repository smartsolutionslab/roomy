using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

public sealed record DisplayName : IValueObject
{
    public string Value { get; }

    private DisplayName(string value) => Value = value;

    public static DisplayName From(string value) =>
        TryParse(value) ?? throw new ArgumentException("DisplayName must not be blank.", nameof(value));

    public static DisplayName? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new DisplayName(value.Trim());
    }

    public override string ToString() => Value;
}
