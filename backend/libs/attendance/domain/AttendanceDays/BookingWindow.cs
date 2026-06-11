namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public static class BookingWindow
{
    public const int WindowDays = 14;

    public static bool IsBookable(BookingDate candidate, BookingDate today)
    {
        var day = candidate.Value;
        var from = today.Value;

        var isWithinWindow = day >= from && day <= from.AddDays(WindowDays);

        return candidate.IsWorkingDay && isWithinWindow;
    }
}
