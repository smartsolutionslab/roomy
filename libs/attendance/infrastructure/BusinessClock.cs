using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure;

public sealed class BusinessClock(TimeProvider time, TimeZoneInfo zone) : IBusinessClock
{
    public DateTimeOffset Now => time.GetUtcNow();

    public BookingDate Today =>
        BookingDate.From(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), zone).DateTime));
}
