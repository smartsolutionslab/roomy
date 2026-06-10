using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// An inclusive span of bookable days [From, To] — the window an occupancy view is computed over
// (FR-001/002/009). From must be on or before To; an inverted span is not a valid value. How *wide* a
// range may be is an occupancy-query policy bounded at the endpoint, not an invariant of the range itself.
public readonly record struct BookingDateRange : IValueObject
{
    public BookingDate From { get; private init; }

    public BookingDate To { get; private init; }

    public static BookingDateRange Between(BookingDate from, BookingDate to) =>
        TryParse(from, to) ?? throw new ArgumentException("A booking date range must start on or before it ends.", nameof(to));

    public static BookingDateRange? TryParse(BookingDate from, BookingDate to)
    {
        if (to.Value < from.Value) return null;
        return new() { From = from, To = to };
    }

    public int LengthInDays => To.Value.DayNumber - From.Value.DayNumber + 1;

    public IEnumerable<BookingDate> Days()
    {
        for (var date = From.Value; date <= To.Value; date = date.AddDays(1))
        {
            yield return BookingDate.From(date);
        }
    }

    public override string ToString() => $"{From}..{To}";
}
