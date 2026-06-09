using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// A single bookable calendar day in the Europe/Berlin zone — the unit a reservation targets and the
// second half of the AttendanceDay stream identity (ADR-0026). Any calendar day is a valid value; the
// bookable-day rules (working day + window) live in BookingWindow, not here. "Today" is computed at
// the application edge and passed in, so the domain reads no ambient clock (research R4).
public readonly record struct BookingDate : IValueObject
{
    public DateOnly Value { get; private init; }

    public static BookingDate From(DateOnly value) => new() { Value = value };

    public static implicit operator DateOnly(BookingDate date) => date.Value;

    public static implicit operator BookingDate(DateOnly value) => From(value);

    public override string ToString() =>
        Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
