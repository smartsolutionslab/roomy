namespace SmartSolutionsLab.Roomy.TestSupport;

// Date helpers for the attendance tests, which pick a deterministic bookable weekday so the booking-window
// rules (Mon-Fri, within the horizon) are exercised independently of the calendar date the test runs on.
public static class BookingDates
{
    public static DateOnly FirstMondayOnOrAfter(DateOnly start)
    {
        var date = start;
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
