using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public readonly record struct BookingDate : IValueObject
{
    public DateOnly Value { get; private init; }

    public static BookingDate From(DateOnly value) => new() { Value = value };

    public static implicit operator DateOnly(BookingDate date) => date.Value;

    public static implicit operator BookingDate(DateOnly value) => From(value);

    public override string ToString() =>
        Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
