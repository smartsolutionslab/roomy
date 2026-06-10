namespace SmartSolutionsLab.Roomy.TestSupport;

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
