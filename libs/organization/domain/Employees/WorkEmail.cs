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
        if (!HasSingleInteriorAt(trimmed, at)) return null;

        var domain = trimmed[(at + 1)..];
        if (!IsDottedDomain(domain)) return null;

        return new WorkEmail(trimmed.ToLowerInvariant());

        static bool HasSingleInteriorAt(string candidate, int at) =>
            at > 0 && at == candidate.LastIndexOf('@') && at != candidate.Length - 1;

        static bool IsDottedDomain(string domain) =>
            domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
    }

    public override string ToString() => Value;
}
