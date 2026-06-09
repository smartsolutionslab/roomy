namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// The bookable-day policy (FR-002/FR-006): a day is bookable only if it is a working day (Mon–Fri,
// Europe/Berlin) and falls within today through today + 14 calendar days, inclusive. Pure and
// stateless — both dates are passed in (research R4), so it is deterministic and the aggregate calls
// it with the application-supplied "today".
public static class BookingWindow
{
    public const int WindowDays = 14;

    public static bool IsBookable(BookingDate candidate, BookingDate today)
    {
        var day = candidate.Value;
        var from = today.Value;

        var isWorkingDay = day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        var isWithinWindow = day >= from && day <= from.AddDays(WindowDays);

        return isWorkingDay && isWithinWindow;
    }
}
