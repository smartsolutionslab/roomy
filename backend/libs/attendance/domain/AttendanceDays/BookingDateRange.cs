using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

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

    public IEnumerable<BookingDate> WorkingDays() => Days().Where(date => date.IsWorkingDay);

    public override string ToString() => $"{From}..{To}";
}
