using System.Text.RegularExpressions;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

public sealed partial record Email : IValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email From(string value) =>
        TryParse(value) ?? throw new ArgumentException("Email is not a valid address.", nameof(value));

    public static Email? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim().ToLowerInvariant();
        if (!EmailPattern().IsMatch(normalized)) return null;

        return new Email(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
