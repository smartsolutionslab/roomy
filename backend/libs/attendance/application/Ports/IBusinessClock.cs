using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IBusinessClock
{
    BookingDate Today { get; }

    DateTimeOffset Now { get; }
}
