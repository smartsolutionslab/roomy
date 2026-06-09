using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// The acting user — the identity-context account behind a request (the BFF token's subject). Attendance
// references it by id only (ADR-0014): a branded GUIDv7. It is resolved to the owning EmployeeIdentifier
// via the Employees read model (003 US4). The implicit conversions keep the EF Core value converter
// trivial.
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
