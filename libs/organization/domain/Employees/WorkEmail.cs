using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

// An employee's work email: required, well-formed, and normalized to trimmed lowercase so equality and
// the credential-side lookup are case-insensitive. Authoritative uniqueness lives on the credential side
// (ADR-0025, research R4); this only rejects malformed input. A deliberately conservative check —
// exactly one '@', a non-empty local part, and a dotted domain.
public sealed record WorkEmail : IValueObject
{
    public string Value { get; }

    private WorkEmail(string value) => Value = value;

    public static WorkEmail From(string value) =>
        TryParse(value) ?? throw new ArgumentException("WorkEmail must be a valid email address.", nameof(value));

    public static WorkEmail? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at != trimmed.LastIndexOf('@') || at == trimmed.Length - 1)
        {
            return null;
        }

        var domain = trimmed[(at + 1)..];
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.'))
        {
            return null;
        }

        return new WorkEmail(trimmed.ToLowerInvariant());
    }

    public override string ToString() => Value;
}
