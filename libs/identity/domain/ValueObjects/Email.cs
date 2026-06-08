using System.Text.RegularExpressions;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

// A user's email: trimmed, lower-cased and syntactically validated at the boundary so the rest of
// the domain can trust it. Equality is by the normalized value; system-wide uniqueness is enforced
// by the aggregate and a DB constraint (FR-009, research R8).
public sealed partial record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        Ensure.That(value).IsNotNullOrWhiteSpace();

        var normalized = value.Trim().ToLowerInvariant();
        Ensure.That(normalized)
            .Satisfies(
                candidate => candidate is not null && EmailPattern().IsMatch(candidate),
                "Email is not a valid address.");

        return new Email(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
