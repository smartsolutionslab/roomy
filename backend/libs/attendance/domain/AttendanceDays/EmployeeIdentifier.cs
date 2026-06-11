using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public readonly record struct EmployeeIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static EmployeeIdentifier New() => new() { Value = Guid.CreateVersion7() };

    public static EmployeeIdentifier From(Guid value) =>
        TryParse(value) ?? throw new ArgumentException("EmployeeIdentifier must not be empty.", nameof(value));

    public static EmployeeIdentifier? TryParse(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(EmployeeIdentifier identifier) => identifier.Value;

    public static implicit operator EmployeeIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
