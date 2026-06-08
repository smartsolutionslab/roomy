using System.Text.RegularExpressions;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// A user's email: trimmed, lower-cased and syntactically validated at the boundary so the rest of
// the domain can trust it. Equality is by the normalized value; system-wide uniqueness is enforced
// by the aggregate and a DB constraint (FR-009, research R8).
public sealed partial record Email : IValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email From(string value) =>
        TryFrom(value) ?? throw new ArgumentException("Email is not a valid address.", nameof(value));

    public static Email? TryFrom(string value)
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
