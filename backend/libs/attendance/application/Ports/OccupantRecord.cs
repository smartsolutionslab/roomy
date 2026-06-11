using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public sealed record OccupantRecord(
    BookingDate Date,
    RoomIdentifier Room,
    EmployeeIdentifier Employee,
    string EmployeeName);
